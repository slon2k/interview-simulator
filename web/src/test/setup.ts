import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach } from 'vitest'

// Required because Testing Library auto-cleanup relies on a global afterEach,
// which Vitest only exposes when globals:true is set.
afterEach(() => {
  cleanup()
})

// jsdom does not implement matchMedia; Mantine's color-scheme hook requires it
Object.defineProperty(window, 'matchMedia', {
  writable: true,
  value: (query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener: () => undefined,
    removeListener: () => undefined,
    addEventListener: () => undefined,
    removeEventListener: () => undefined,
    dispatchEvent: () => false,
  }),
})

// jsdom does not implement ResizeObserver; Mantine components require it
window.ResizeObserver = class ResizeObserver {
  observe() {}
  unobserve() {}
  disconnect() {}
}

// jsdom does not implement scrollIntoView; Mantine combobox calls it when opening a dropdown
window.HTMLElement.prototype.scrollIntoView = function () {}
