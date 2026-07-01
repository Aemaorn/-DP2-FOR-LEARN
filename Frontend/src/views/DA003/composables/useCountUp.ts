import { ref, watch, onMounted, type Ref } from 'vue'

export function useCountUp(target: Ref<number>, duration = 600) {
  const display = ref(0)
  let raf: number | null = null
  let mounted = false

  function animate(from: number, to: number) {
    if (raf) cancelAnimationFrame(raf)
    if (from === to) { display.value = to; return }
    const startTime = performance.now()

    function step(now: number) {
      const elapsed = now - startTime
      const progress = Math.min(elapsed / duration, 1)
      const ease = 1 - Math.pow(1 - progress, 3)
      display.value = Math.round(from + (to - from) * ease)
      if (progress < 1) raf = requestAnimationFrame(step)
    }

    raf = requestAnimationFrame(step)
  }

  onMounted(() => {
    mounted = true
    animate(0, target.value)
  })

  watch(target, (newVal, oldVal) => {
    if (!mounted) return
    animate(oldVal ?? 0, newVal)
  })

  return display
}
