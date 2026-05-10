import '@testing-library/jest-dom/vitest';
import { cleanup, configure } from '@testing-library/react';
import { afterEach } from 'vitest';

// The default 1s for findBy* is tight when the whole suite runs in parallel on
// a busy CI runner; a few tests were failing now and then for that alone.
configure({ asyncUtilTimeout: 5000 });

afterEach(() => {
  cleanup();
  localStorage.clear();
});
