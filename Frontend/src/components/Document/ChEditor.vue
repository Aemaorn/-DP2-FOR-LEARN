<script setup lang="ts">
import cookie from '@/configs/cookie';
import { ref, onMounted, onBeforeUnmount, watch, computed, nextTick } from 'vue';
import { useAuthenticationStore } from '@/stores/authentication';
import { v4 as uuidv4 } from 'uuid';
import { Select, Button } from 'primevue';
import { ToDateTime } from '@/helpers/dateTime';
import { showConfirmDialogAsync } from '@/helpers/dialog';
import type { DocumentVersion } from '@/models/shared/document';
import { DepartmentId } from '@/enums/businessUnit';

const SAVE_TIMEOUT_MS = 10000;

const apiUrl = import.meta.env.VITE_APP_API_URL;
const docsService = import.meta.env.VITE_APP_API_DOCS_URL;
const wopiDocsUrl = import.meta.env.VITE_APP_WOPI_DOCS_URL;
const key = uuidv4();

interface Props {
  docId: string;
  docName: string;
  readonly: boolean;
  width?: string;
  height?: string;
  save?: () => void;
  versions?: DocumentVersion[];
  canRestoreVersion?: boolean;
}

const props = withDefaults(defineProps<Props>(), {
  width: '100%',
  height: '1000',
  canRestoreVersion: false,
});

const emit = defineEmits<{
  restoreVersion: [];
}>();

const isRestoring = ref(false);

const auth = useAuthenticationStore();
const iframeRef = ref<HTMLIFrameElement | null>(null);
const iframeContainerRef = ref<HTMLDivElement | null>(null);
const formRef = ref<HTMLFormElement | null>(null);
const isExpanded = ref(false);

const forceEdit = ref(false);

const canForceEdit = computed<boolean>(
  () => auth.profile.departmentCode === DepartmentId.JorPor
);

const effectiveReadonly = computed<boolean>(
  () => props.readonly && !(forceEdit.value && canForceEdit.value)
);

const resolveCurrentVersionFileId = (): string => {
  const current = props.versions?.find(v => v.isCurrent);
  return current?.fileId ?? props.docId;
};

// Resolve the actual file to load in WOPI
// Editable (effectiveReadonly=false): isCurrent → working copy (props.docId) so user can edit
// Readonly (effectiveReadonly=true): isCurrent → version snapshot directly (has replaced placeholders)
const resolveWopiFileId = (versionFileId: string): string => {
  const version = props.versions?.find(v => v.fileId === versionFileId);
  if (forceEdit.value && canForceEdit.value) {
    return versionFileId;
  }
  return (version?.isCurrent && !effectiveReadonly.value) ? props.docId : versionFileId;
};

const selectedVersionFileId = ref<string>(resolveCurrentVersionFileId());
const currentLoadedWopiFileId = ref<string>('');
const isFirstLoad = ref(true);

const versionOptions = computed(() =>
  props.versions?.map(v => ({
    label: `v${v.version} - ${ToDateTime(v.createdAt)}${v.isCurrent ? ' (ปัจจุบัน)' : ''}`,
    value: v.fileId,
    isCurrent: v.isCurrent
  })) ?? []
);

const toggleExpand = (): void => {
  isExpanded.value = !isExpanded.value;
};

const isViewingOldVersion = computed<boolean>(() => {
  const current = props.versions?.find(v => v.isCurrent);
  if (!current) return false;
  return selectedVersionFileId.value !== current.fileId;
});

const canShowEditToggle = computed<boolean>(
  () => canForceEdit.value && (props.readonly || isViewingOldVersion.value)
);

const toggleEditMode = async (): Promise<void> => {
  forceEdit.value = !forceEdit.value;
  await loadDocument(selectedVersionFileId.value);
};

const confirmRestoreVersion = async (): Promise<void> => {
  const confirmed = await showConfirmDialogAsync(
    undefined,
    `ต้องการ รีเซ็ตเอกสาร ใช่หรือไม่`,
    'ยกเลิก',
    'ยืนยัน'
  );

  if (confirmed) {
    isRestoring.value = true;
    try {
      emit('restoreVersion');
    } finally {
      isRestoring.value = false;
    }
  }
};

const onVersionChange = async (event: { value: string }): Promise<void> => {
  selectedVersionFileId.value = event.value;
  await loadDocument(event.value);
};

const recreateIframe = (docId: string): void => {
  // Use container ref instead of iframe's parentNode to avoid stale ref issues
  if (iframeContainerRef.value) {
    // Remove all existing iframes from container
    const existingIframes = iframeContainerRef.value.querySelectorAll('iframe');
    existingIframes.forEach(iframe => iframe.remove());

    // Create new iframe element
    const newIframe = document.createElement('iframe');
    const iframeName = `iframe-${docId}-${key}`;
    newIframe.name = iframeName;
    newIframe.id = iframeName;
    newIframe.title = props.docName;
    newIframe.height = isExpanded.value ? '100%' : props.height;
    newIframe.width = props.width;
    newIframe.allowFullscreen = true;
    newIframe.className = 'collabora-iframe';

    // Prevent browser auto-scroll on focus
    attachIframeScrollPrevention(newIframe);

    // Append new iframe to container
    iframeContainerRef.value.appendChild(newIframe);

    // Update ref
    iframeRef.value = newIframe;
  }
};

const loadDocument = async (versionFileId: string): Promise<void> => {
  if (formRef.value) {
    // isCurrent → load working copy (props.docId), old version → load snapshot
    const wopiFileId = resolveWopiFileId(versionFileId);

    let targetName: string;

    // On first load, use the template iframe (its name is based on props.docId)
    // On subsequent loads (version changes), recreate the iframe with new name
    if (isFirstLoad.value) {
      isFirstLoad.value = false;
      targetName = `iframe-${props.docId}-${key}`;
    } else {
      targetName = `iframe-${wopiFileId}-${key}`;
      recreateIframe(wopiFileId);
      // Wait for DOM to update after iframe recreation
      await nextTick();
    }

    const wopiUrl = wopiDocsUrl ?? `${apiUrl}/api/wopi/files/`;
    const isSecretEditing = forceEdit.value && canForceEdit.value;
    const isOldVersion = wopiFileId !== props.docId;
    const isReadonly = isSecretEditing ? false : (effectiveReadonly.value || isOldVersion);
    const url = `${wopiUrl}${wopiFileId}?readOnly=${isReadonly}&userId=${auth.profile.id}`;
    const encodedUrl = encodeURIComponent(url);
    const actionUrl = `${docsService}/cool.html?WOPISrc=${encodedUrl}`;

    formRef.value.target = targetName;
    formRef.value.action = actionUrl;

    // Save scroll position before form submit (browser may scroll to iframe target)
    const scrollX = window.scrollX;
    const scrollY = window.scrollY;
    formRef.value.submit();
    requestAnimationFrame(() => {
      window.scrollTo(scrollX, scrollY);
    });

    currentLoadedWopiFileId.value = wopiFileId;
  }
};

const onSetDocumentTor = (docId: string): void => {
  loadDocument(docId);
};

const post = (msg: any): void => {
  const json = JSON.stringify(msg);

  // Use iframeRef directly instead of getElementById to avoid duplicate ID issues
  if (iframeRef.value?.contentWindow) {
    iframeRef.value.contentWindow.postMessage(json, docsService);
  }
};

const handleSave = (): void => {
  post({
    MessageId: 'Action_Save',
    Values: { Notify: true },
  });
};

// Save and wait for Collabora to finish saving before calling callback
let pendingSaveCallback: (() => void) | null = null;
let pendingSaveTimeoutId: ReturnType<typeof setTimeout> | null = null;

const saveAndWait = (callback: () => void): void => {
  pendingSaveCallback = callback;
  pendingSaveTimeoutId = setTimeout(() => {
    if (pendingSaveCallback === callback) {
      pendingSaveCallback = null;
      pendingSaveTimeoutId = null;
      callback();
    }
  }, SAVE_TIMEOUT_MS);
  post({
    MessageId: 'Action_Save',
    Values: { Notify: true },
  });
};

const receiveMessage = (event: any): void => {
  if (typeof event.data !== 'string') {
    return;
  }

  // Only handle messages from our own iframe
  if (iframeRef.value?.contentWindow && event.source !== iframeRef.value.contentWindow) {
    return;
  }

  const msg = JSON.parse(event.data);

  if (!msg) {
    return;
  }

  switch (msg.MessageId) {
    case 'App_LoadingStatus':
      if (msg.Values && msg.Values.Status === 'Document_Loaded') {
        post({ MessageId: 'Host_PostmessageReady' });
        post({ MessageId: 'Hide_Sidebar' });
      }
      break;

    case 'Doc_ModifiedStatus':
      break;

    case 'UI_Save':
      if (props.save && !(forceEdit.value && canForceEdit.value)) {
        props.save();
      }
      break;

    case 'Action_Save_Resp':
      // Collabora finished saving, call pending callback if exists
      // Add a delay to ensure file is fully persisted to storage before continuing
      if (pendingSaveCallback) {
        const callback = pendingSaveCallback;
        pendingSaveCallback = null;
        if (pendingSaveTimeoutId) {
          clearTimeout(pendingSaveTimeoutId);
          pendingSaveTimeoutId = null;
        }
        setTimeout(() => callback(), 1000);
      }
      break;

    default:
      break;
  }
};

// Exposing methods for parent components
defineExpose({
  clickSave: handleSave,
  saveAndWait,
  setPlaceholderInDocument: (text: string, hint?: string): void =>
    setPlaceholderInDocument(text, hint),
});

watch(
  () => formRef.value,
  async (newFormRef): Promise<void> => {
    if (newFormRef) {
      // Wait for next tick to ensure iframe is rendered in DOM
      await nextTick();
      onSetDocumentTor(resolveCurrentVersionFileId());
    }
  },
  { immediate: true }
);

const setPlaceholderInDocument = (placeholderName: string, hint?: string): void => {
  // Call the existing InsertText macro
  post({
    MessageId: 'CallPythonScript',
    SendTime: Date.now(),
    ScriptFile: 'InsertPlaceholderField.py',
    Function: 'InsertJumpEditPlaceholder',
    Values: {
      placeholder_name: {
        type: 'string',
        value: placeholderName,
      },
      placeholder_type: {
        type: 'string',
        value: 'TEXT',
      },
      hint: {
        type: 'string',
        value: hint ?? `${placeholderName} details`,
      },
    },
  });
};

// Handle ESC key to exit expanded mode
const handleKeydown = (event: KeyboardEvent): void => {
  if (event.key === 'Escape' && isExpanded.value) {
    isExpanded.value = false;
  }
};

// Prevent browser from auto-scrolling when iframe receives focus
// Strategy: save scroll position when mouse enters iframe area (before click),
// then restore it when parent window loses focus to the iframe.
const scrollSnapshot = { x: 0, y: 0 };

const handleIframeMouseEnter = (): void => {
  scrollSnapshot.x = window.scrollX;
  scrollSnapshot.y = window.scrollY;
};

const handleWindowBlur = (): void => {
  requestAnimationFrame(() => {
    // Only restore scroll if our iframe actually received the focus
    if (document.activeElement === iframeRef.value) {
      window.scrollTo(scrollSnapshot.x, scrollSnapshot.y);
    }
  });
};

const attachIframeScrollPrevention = (iframe: HTMLIFrameElement): void => {
  iframe.addEventListener('mouseenter', handleIframeMouseEnter);
};

const detachIframeScrollPrevention = (iframe: HTMLIFrameElement): void => {
  iframe.removeEventListener('mouseenter', handleIframeMouseEnter);
};

// Setup event listeners
onMounted((): void => {
  window.addEventListener('message', receiveMessage, false);
  window.addEventListener('keydown', handleKeydown);
  window.addEventListener('blur', handleWindowBlur);
  if (iframeRef.value) {
    attachIframeScrollPrevention(iframeRef.value);
  }
});

// Clean up event listeners
onBeforeUnmount((): void => {
  window.removeEventListener('message', receiveMessage);
  window.removeEventListener('keydown', handleKeydown);
  window.removeEventListener('blur', handleWindowBlur);
  if (iframeRef.value) {
    detachIframeScrollPrevention(iframeRef.value);
  }
});

watch(
  () => props.docId,
  (newValue, oldValue): void => {
    selectedVersionFileId.value = resolveCurrentVersionFileId();
    forceEdit.value = false;
    // Only reload document if props.docId actually changed (not on initial mount)
    // Initial mount is handled by the formRef watch
    if (oldValue !== undefined && oldValue !== newValue) {
      onSetDocumentTor(selectedVersionFileId.value);
    }
  },
  {
    immediate: true,
  }
);

watch(
  () => props.versions,
  (versions): void => {
    if (versions?.length) {
      const current = versions.find(v => v.isCurrent);
      if (current && current.fileId !== selectedVersionFileId.value) {
        selectedVersionFileId.value = current.fileId;
        // Only reload iframe if the actual WOPI file changes
        // isCurrent versions always resolve to props.docId (working copy),
        // so after save (new version created) no reload is needed
        const newWopiFileId = resolveWopiFileId(current.fileId);
        if (newWopiFileId !== currentLoadedWopiFileId.value) {
          loadDocument(current.fileId);
        }
      }
    }
  },
);
</script>

<template>
  <form ref="formRef" :target="`iframe-${docId}-${key}`" method="post" encType="multipart/form-data"
    name="collaboraForm">
    <input name="access_token" :value="cookie.get('access-token')" type="hidden" />
    <input name="ui_defaults" value="UIMode=compact;TextSidebar=false;SavedUIState=false" type="hidden" />
  </form>

  <div class="collabora-wrapper" :class="{ 'is-expanded': isExpanded }">
    <!-- Top bar with version selector and expand/collapse button -->
    <div class="toolbar">
      <!-- Version dropdown (show only if versions available and more than 1) -->
      <div class="version-selector" v-if="versions && versions.length > 1">
        <Select v-model="selectedVersionFileId" :options="versionOptions" optionLabel="label" optionValue="value"
          placeholder="เลือก Version" class="version-select" @change="onVersionChange" />
        <Button v-if="canRestoreVersion" type="button" label="รีเซ็ตเอกสาร" icon="pi pi-history" variant="outlined"
          severity="warn" size="small" :loading="isRestoring" @click="confirmRestoreVersion" />
        <button v-if="canShowEditToggle" type="button" class="edit-mode-btn"
          :class="{ 'is-editing': forceEdit }" @click="toggleEditMode">
          <span class="material-symbols-outlined">
            {{ forceEdit ? 'lock' : 'edit' }}
          </span>
        </button>
      </div>

      <button type="button" class="expand-btn" @click="toggleExpand"
        :title="isExpanded ? 'ย่อหน้าจอ (ESC)' : 'ขยายเต็มหน้าจอ'">
        <span class="material-symbols-outlined">
          {{ isExpanded ? 'close_fullscreen' : 'open_in_full' }}
        </span>
        {{ isExpanded ? 'ย่อหน้าจอ' : 'ขยายเต็มหน้าจอ' }}
      </button>
    </div>

    <div ref="iframeContainerRef" class="iframe-container"
      :style="{ minHeight: isExpanded ? undefined : `${props.height}px` }">
      <iframe :name="`iframe-${docId}-${key}`" :id="`iframe-${docId}-${key}`" :title="props.docName" ref="iframeRef"
        :height="isExpanded ? '100%' : props.height" :width="props.width" allowfullscreen class="collabora-iframe" />
    </div>
  </div>
</template>

<style scoped>
.collabora-wrapper {
  display: flex;
  flex-direction: column;
  width: 100%;
  transition: all 0.3s ease;
}

.collabora-wrapper.is-expanded {
  position: fixed;
  inset: 0;
  z-index: 9999;
  background: white;
  height: 100vh;
  height: 100dvh;
  /* Dynamic viewport height for mobile */
}

.toolbar {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 8px;
  padding: 8px 16px;
  background: linear-gradient(135deg, #f8fafc 0%, #f1f5f9 100%);
  border-bottom: 1px solid #e2e8f0;
  flex-shrink: 0;
}

.expand-btn {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 6px 12px;
  background: white;
  border: 1px solid #cbd5e0;
  border-radius: 6px;
  cursor: pointer;
  font-size: 13px;
  color: #475569;
  transition: all 0.2s ease;
}

.expand-btn:hover {
  background: #f1f5f9;
  border-color: #94a3b8;
  color: #1e293b;
}

.expand-btn .material-symbols-outlined {
  font-size: 18px;
}

.edit-mode-btn {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 4px;
  background: transparent;
  border: none;
  border-radius: 6px;
  cursor: pointer;
  color: #475569;
  opacity: 0;
  transition: opacity 0.2s ease;
}

.edit-mode-btn:hover {
  opacity: 1;
}

.edit-mode-btn .material-symbols-outlined {
  font-size: 18px;
}

.edit-mode-btn.is-editing {
  color: #16a34a;
  opacity: 1;
}

.edit-mode-btn.is-editing:hover {
  background: #dcfce7;
}

.is-expanded .expand-btn {
  background: #fee2e2;
  border-color: #fca5a5;
  color: #dc2626;
}

.is-expanded .expand-btn:hover {
  background: #fecaca;
  border-color: #f87171;
}

.iframe-container {
  flex: 1;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.collabora-iframe {
  flex: 1;
  width: 100%;
  border: none;
  min-height: 0;
  /* Allow flex item to shrink */
}

.is-expanded .iframe-container,
.is-expanded .collabora-iframe {
  height: 100%;
}

.version-selector {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-right: auto;
}

.version-select {
  min-width: 280px;
}

.readonly-badge {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 4px 8px;
  background: #fef3c7;
  border: 1px solid #f59e0b;
  border-radius: 4px;
  font-size: 12px;
  color: #92400e;
}

.readonly-badge .material-symbols-outlined {
  font-size: 14px;
}

/* Override PrimeVue Select highlight colors */
.version-select :deep(.p-select-option.p-select-option-selected),
.version-select :deep(.p-select-option.p-focus) {
  background: #dbeafe !important;
  color: #1e40af !important;
}

.version-select :deep(.p-select-option:hover) {
  background: #eff6ff !important;
}
</style>
