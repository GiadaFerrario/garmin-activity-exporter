import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import IconButton from '@/components/IconButton.vue'

describe('IconButton', () => {
  it.each([
    ['file', 'i-lucide-file-text'],
    ['copy', 'i-lucide-copy'],
    ['close', 'i-lucide-x'],
    ['trash', 'i-lucide-trash-2'],
    ['refresh', 'i-lucide-refresh-cw'],
  ] as const)('draws the icon %s', (icon, iconClass) => {
    const wrapper = mount(IconButton, { props: { icon, label: 'A label' } })

    expect(wrapper.get('span').classes()).toContain(iconClass)
  })

  it('has the label as its accessible name and as its tooltip', () => {
    const wrapper = mount(IconButton, { props: { icon: 'copy', label: 'Copy' } })

    expect(wrapper.attributes('aria-label')).toBe('Copy')
    expect(wrapper.attributes('title')).toBe('Copy')
  })

  it('is a button that does not submit a form', () => {
    expect(mount(IconButton, { props: { icon: 'copy', label: 'Copy' } }).attributes('type')).toBe(
      'button',
    )
  })

  it('can be disabled', () => {
    const wrapper = mount(IconButton, { props: { icon: 'trash', label: 'Delete', disabled: true } })

    expect(wrapper.attributes('disabled')).toBeDefined()
  })
})
