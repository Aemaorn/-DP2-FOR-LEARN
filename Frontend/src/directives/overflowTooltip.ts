import type { Directive } from 'vue';

const vOverflowTooltip: Directive<HTMLElement, string | undefined> = {
  mounted(el, binding) {
    let tooltipEl: HTMLDivElement | null = null;

    const show = () => {
      if (el.scrollWidth <= el.clientWidth) return;
      if (tooltipEl) return;

      tooltipEl = document.createElement('div');
      tooltipEl.className = 'overflow-tooltip';
      tooltipEl.textContent = binding.value ?? el.textContent?.trim() ?? '';
      document.body.appendChild(tooltipEl);

      const rect = el.getBoundingClientRect();
      const tooltipRect = tooltipEl.getBoundingClientRect();
      tooltipEl.style.left = `${rect.left + rect.width / 2 - tooltipRect.width / 2}px`;
      tooltipEl.style.top = `${rect.top - tooltipRect.height - 6}px`;
    };

    const hide = () => {
      tooltipEl?.remove();
      tooltipEl = null;
    };

    el.addEventListener('mouseenter', show);
    el.addEventListener('mouseleave', hide);

    (el as any).__ovtHandlers = { show, hide };
  },

  updated(el, binding) {
    (el as any).__ovtValue = binding.value;
  },

  unmounted(el) {
    const h = (el as any).__ovtHandlers;
    if (h) {
      el.removeEventListener('mouseenter', h.show);
      el.removeEventListener('mouseleave', h.hide);
    }
  },
};

export { vOverflowTooltip };
