import { ref, onMounted, onUnmounted, type Ref, nextTick } from 'vue';
import type { SectionItem } from '../sectionConfig';

const SECTION_SELECTOR = '[data-section-id]';
const SCROLL_OFFSET = 100;

export function useSectionSpy(containerRef: Ref<HTMLElement | null>) {
  const sections = ref<SectionItem[]>([]);
  const activeSectionId = ref<string>('');

  let intersectionObserver: IntersectionObserver | null = null;
  let mutationObserver: MutationObserver | null = null;
  let isScrolling = false;
  let scrollDebounce: ReturnType<typeof setTimeout> | null = null;
  let scrollHandler: (() => void) | null = null;

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

    setupIntersectionObserver();
  };

  const detectActiveSection = () => {
    const container = containerRef.value;
    if (!container) return;

    const elements = container.querySelectorAll<HTMLElement>(SECTION_SELECTOR);
    let active: string | null = null;

    for (const el of elements) {
      const rect = el.getBoundingClientRect();
      if (rect.top <= SCROLL_OFFSET + 10) {
        active = el.dataset.sectionId ?? null;
      } else {
        break;
      }
    }

    if (active) {
      activeSectionId.value = active;
    }
  };

  const setupIntersectionObserver = () => {
    if (intersectionObserver) {
      intersectionObserver.disconnect();
    }

    const container = containerRef.value;
    if (!container) return;

    intersectionObserver = new IntersectionObserver(
      () => {
        if (isScrolling) return;
        detectActiveSection();
      },
      {
        rootMargin: '-80px 0px -60% 0px',
        threshold: [0, 0.1, 0.25, 0.5],
      }
    );

    const elements = container.querySelectorAll<HTMLElement>(SECTION_SELECTOR);
    elements.forEach((el) => intersectionObserver!.observe(el));
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

  onMounted(() => {
    nextTick(() => {
      refreshSections();
      setupMutationObserver();
    });
  });

  onUnmounted(() => {
    intersectionObserver?.disconnect();
    mutationObserver?.disconnect();
    cleanupScrollListener();
  });

  return {
    sections,
    activeSectionId,
    scrollToSection,
    refreshSections,
  };
}
