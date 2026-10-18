import { useEffect } from 'react';

/** Sets the browser tab title while the page is shown, e.g. "Website relaunch · Taskboard". */
export function usePageTitle(title: string | undefined) {
  useEffect(() => {
    document.title = title ? `${title} · Taskboard` : 'Taskboard';
    return () => {
      document.title = 'Taskboard';
    };
  }, [title]);
}
