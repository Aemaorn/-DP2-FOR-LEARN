import { ref, onUnmounted, type Ref, nextTick } from 'vue';
import type { SectionItem } from '../sectionConfig';

const SECTION_SELECTOR = '[data-section-id]';
const SCROLL_OFFSET = 100;

export function useSectionSpy(containerRef: Ref<HTMLElement | null>) {
  const sections = ref<SectionItem[]>([]);
  const activeSectionId = ref<string>('');

  let mutationObserver: MutationObserver | null = null;
  let isScrolling = false;
  let isInitializing = false;
  let initTimeout: ReturnType<typeof setTimeout> | null = null;
  let scrollDebounce: ReturnType<typeof setTimeout> | null = null;
  let scrollHandler: (() => void) | null = null;
  let scrollSpyHandler: (() => void) | null = null;
  let scrollSpyDebounce: ReturnType<typeof setTimeout> | null = null;

  const refreshSections = () => {
    const container = containerRef.value;
    if (!container) return;

    const elements = container.querySelectorAll<HTMLElement>(SECTION_SELECTOR);
    const items: SectionItem[] = [];

    elements.forEach((el) => {
      const id = el.dataset.sectionId;
      const label = el.dataset.sectionLabel;
      if (id && label) {
        items.push({ id, label });
      }
    });

    sections.value = items;

    if (items.length > 0 && !activeSectionId.value) {
      activeSectionId.value = items[0].id;
    }
  };

  const detectActiveSection = () => {
    const container = containerRef.value;
    if (!container) return;

    const elements = container.querySelectorAll<HTMLElement>(SECTION_SELECTOR);
    if (elements.length === 0) return;

    // Find the last section whose top has scrolled past the offset threshold.
    // We use a generous margin (half the viewport height) so that the section
    // whose content is currently most visible gets highlighted, rather than
    // requiring its top edge to be almost at the very top of the viewport.
    const threshold = window.innerHeight / 3;
    let active: string | null = null;

    for (const el of elements) {
      const rect = el.getBoundingClientRect();
      if (rect.top <= threshold) {
        active = el.dataset.sectionId ?? null;
      } else {
        break;
      }
    }

    if (active) {
      activeSectionId.value = active;
    }
  };

  const setupScrollSpy = () => {
    cleanupScrollSpy();

    scrollSpyHandler = () => {
      if (isScrolling || isInitializing) return;
      if (scrollSpyDebounce) clearTimeout(scrollSpyDebounce);
      scrollSpyDebounce = setTimeout(() => {
        detectActiveSection();
      }, 50);
    };

    window.addEventListener('scroll', scrollSpyHandler, { passive: true });
  };

  const cleanupScrollSpy = () => {
    if (scrollSpyDebounce) clearTimeout(scrollSpyDebounce);
    if (scrollSpyHandler) {
      window.removeEventListener('scroll', scrollSpyHandler);
      scrollSpyHandler = null;
    }
  };

  const cleanupScrollListener = () => {
    if (scrollDebounce) clearTimeout(scrollDebounce);
    if (scrollHandler) {
      window.removeEventListener('scroll', scrollHandler);
      scrollHandler = null;
    }
  };

  const scrollToSection = (id: string) => {
    const container = containerRef.value;
    if (!container) return;

    const target = container.querySelector<HTMLElement>(`[data-section-id="${id}"]`);
    if (!target) return;

    cleanupScrollListener();
    isScrolling = true;
    activeSectionId.value = id;

    scrollHandler = () => {
      if (scrollDebounce) clearTimeout(scrollDebounce);
      scrollDebounce = setTimeout(() => {
        isScrolling = false;
        cleanupScrollListener();
      }, 150);
    };

    window.addEventListener('scroll', scrollHandler, { passive: true });

    const top = target.getBoundingClientRect().top + window.scrollY - SCROLL_OFFSET;
    window.scrollTo({ top, behavior: 'smooth' });

    // Kick off the initial debounce in case the page is already at position
    scrollHandler();
  };

  const setupMutationObserver = () => {
    const container = containerRef.value;
    if (!container) return;

    mutationObserver = new MutationObserver(() => {
      nextTick(() => refreshSections());
    });

    mutationObserver.observe(container, {
      childList: true,
      subtree: true,
      attributes: true,
      attributeFilter: ['data-section-id', 'data-section-label'],
    });
  };

  const cleanup = () => {
    mutationObserver?.disconnect();
    mutationObserver = null;
    if (initTimeout) { clearTimeout(initTimeout); initTimeout = null; }
    isInitializing = false;
    cleanupScrollSpy();
    cleanupScrollListener();
  };

  const init = () => {
    cleanup();
    isInitializing = true;
    nextTick(() => {
      refreshSections();
      setupMutationObserver();
      setupScrollSpy();
      // Suppress scroll-spy detection until layout fully settles
      initTimeout = setTimeout(() => {
        isInitializing = false;
        detectActiveSection();
      }, 500);
    });
  };

  const reinit = () => {
    activeSectionId.value = '';
    init();
  };

  onUnmounted(() => {
    cleanup();
  });

  return {
    sections,
    activeSectionId,
    scrollToSection,
    refreshSections,
    reinit,
  };
}
