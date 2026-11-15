import { describe, expect, it } from 'vitest'
import { mount } from '@vue/test-utils'
import ConfirmPopup from '@/components/ConfirmPopup.vue'

function create(busy = false) {
  return mount(ConfirmPopup, {
    props: {
      title: 'Delete this activity?',
      message: 'Morning run will not come back.',
      confirmLabel: 'Delete',
      busy,
    },
  })
}

describe('ConfirmPopup', () => {
  it('shows the question and the words of the button', () => {
    const wrapper = create()

    expect(wrapper.text()).toContain('Delete this activity?')
    expect(wrapper.text()).toContain('Morning run will not come back.')
    expect(wrapper.get('[data-test="confirm"]').text()).toBe('Delete')
  })

  it('says it is confirmed when the button is pressed', async () => {
    const wrapper = create()

    await wrapper.get('[data-test="confirm"]').trigger('click')

    expect(wrapper.emitted('confirm')).toHaveLength(1)
    expect(wrapper.emitted('cancel')).toBeUndefined()
  })

  it('says it is cancelled with the cancel button', async () => {
    const wrapper = create()

    await wrapper.get('[data-test="cancel"]').trigger('click')

    expect(wrapper.emitted('cancel')).toHaveLength(1)
    expect(wrapper.emitted('confirm')).toBeUndefined()
  })

  it('says it is cancelled when the dark area around it is pressed', async () => {
    const wrapper = create()

    await wrapper.get('[data-test="backdrop"]').trigger('click')

    expect(wrapper.emitted('cancel')).toHaveLength(1)
  })

  it('cannot be pressed twice while it is working', () => {
    const wrapper = create(true)

    expect(wrapper.get('[data-test="confirm"]').attributes('disabled')).toBeDefined()
    expect(wrapper.get('[data-test="cancel"]').attributes('disabled')).toBeDefined()
  })
})
