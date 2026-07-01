import { useBreakpoints, breakpointsTailwind, type Breakpoints } from '@vueuse/core';

export const tailwindBreakpoint = () => {
  const breakpoints = useBreakpoints(breakpointsTailwind);

  const smAndDown = breakpoints.smallerOrEqual('sm');
  const smAndUp = breakpoints.greater('sm');
  const mdAndDown = breakpoints.smallerOrEqual('md');
  const mdAndUp = breakpoints.greater('md');
  const lgAndDown = breakpoints.smallerOrEqual('lg');
  const lgAndUp = breakpoints.greater('lg');
  const xlAndDown = breakpoints.smallerOrEqual('xl');
  const xlAndUp = breakpoints.greater('xl');
  const xxlAndDown = breakpoints.smallerOrEqual('2xl');
  const xxlAndUp = breakpoints.greater('2xl');
  const current = breakpoints.active();

  const between = (start: keyof Breakpoints, end: keyof Breakpoints) => {
    return breakpoints.between(start as any, end as any);
  };

  return {
    smAndDown,
    smAndUp,
    mdAndDown,
    mdAndUp,
    lgAndDown,
    lgAndUp,
    xlAndDown,
    xlAndUp,
    xxlAndDown,
    xxlAndUp,
    current,
    between,
  };
};
