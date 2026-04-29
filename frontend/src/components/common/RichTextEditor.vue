<script setup lang="ts">
/**
 * RichTextEditor – Reusable TipTap-based rich text editor component.
 *
 * Features:
 * - Bold, Italic, Underline, Strikethrough
 * - Headings (H1–H3)
 * - Ordered / Unordered / Task lists
 * - Text alignment (left, center, right, justify)
 * - Tables (insert, add/delete rows/columns)
 * - Links, Images
 * - Text color & highlight
 * - Undo / Redo
 * - RTL / LTR support
 * - Placeholder text
 * - Read-only mode
 */
import { watch, onBeforeUnmount, computed } from 'vue'
import { useEditor, EditorContent } from '@tiptap/vue-3'
import StarterKit from '@tiptap/starter-kit'
import { Table } from '@tiptap/extension-table'
import TableRow from '@tiptap/extension-table-row'
import TableCell from '@tiptap/extension-table-cell'
import TableHeader from '@tiptap/extension-table-header'
import TextAlign from '@tiptap/extension-text-align'
import Underline from '@tiptap/extension-underline'
import { TextStyle } from '@tiptap/extension-text-style'
import Color from '@tiptap/extension-color'
import Highlight from '@tiptap/extension-highlight'
import Link from '@tiptap/extension-link'
import Image from '@tiptap/extension-image'
import Placeholder from '@tiptap/extension-placeholder'
import TaskList from '@tiptap/extension-task-list'
import TaskItem from '@tiptap/extension-task-item'
import { useI18n } from 'vue-i18n'

const props = withDefaults(defineProps<{
  modelValue: string
  placeholder?: string
  editable?: boolean
  minHeight?: string
  maxHeight?: string
  dir?: 'rtl' | 'ltr' | 'auto'
  compact?: boolean
  borderless?: boolean
}>(), {
  placeholder: '',
  editable: true,
  minHeight: '200px',
  maxHeight: '500px',
  dir: 'auto',
  compact: false,
  borderless: false,
})

const emit = defineEmits<{
  'update:modelValue': [value: string]
  'update:text': [value: string]
}>()

const { locale } = useI18n()
const isRtl = computed(() => props.dir === 'rtl' || (props.dir === 'auto' && locale.value === 'ar'))
const editorContainerClass = computed(() => {
  if (props.borderless && !props.editable) {
    return 'rich-text-editor overflow-hidden bg-transparent transition-all'
  }

  return 'rich-text-editor overflow-hidden rounded-lg border border-surface-dim bg-white transition-all focus-within:border-primary focus-within:ring-2 focus-within:ring-primary/20'
})

const editor = useEditor({
  content: props.modelValue || '',
  editable: props.editable,
  extensions: [
    StarterKit.configure({
      heading: { levels: [1, 2, 3] },
      bulletList: { keepMarks: true },
      orderedList: { keepMarks: true },
    }),
    Table.configure({ resizable: true }),
    TableRow,
    TableCell,
    TableHeader,
    TextAlign.configure({ types: ['heading', 'paragraph'] }),
    Underline,
    TextStyle,
    Color,
    Highlight.configure({ multicolor: true }),
    Link.configure({ openOnClick: false, HTMLAttributes: { class: 'text-primary underline' } }),
    Image.configure({ inline: true }),
    Placeholder.configure({ placeholder: props.placeholder }),
    TaskList,
    TaskItem.configure({ nested: true }),
  ],
  onUpdate: ({ editor: ed }) => {
    const html = ed.getHTML()
    const text = ed.getText()
    emit('update:modelValue', html)
    emit('update:text', text)
  },
})

// Sync external changes
watch(() => props.modelValue, (newVal) => {
  if (editor.value && newVal !== editor.value.getHTML()) {
    editor.value.commands.setContent(newVal || '', { emitUpdate: false })
  }
})

watch(() => props.editable, (val) => {
  editor.value?.setEditable(val)
})

onBeforeUnmount(() => {
  editor.value?.destroy()
})

/* ── Toolbar helpers ────────────────────────────────── */
function setLink() {
  const previousUrl = editor.value?.getAttributes('link').href || ''
  const url = window.prompt('URL', previousUrl)
  if (url === null) return
  if (url === '') {
    editor.value?.chain().focus().extendMarkRange('link').unsetLink().run()
    return
  }
  editor.value?.chain().focus().extendMarkRange('link').setLink({ href: url }).run()
}

function insertTable() {
  editor.value?.chain().focus().insertTable({ rows: 3, cols: 3, withHeaderRow: true }).run()
}

function insertImage() {
  const url = window.prompt('Image URL')
  if (url) {
    editor.value?.chain().focus().setImage({ src: url }).run()
  }
}

/* ── Toolbar button groups ──────────────────────────── */
interface ToolbarButton {
  icon: string
  title: string
  action: () => void
  isActive?: () => boolean
  disabled?: () => boolean
}

const textFormatButtons = computed<ToolbarButton[]>(() => [
  { icon: 'pi-bold', title: 'Bold', action: () => editor.value?.chain().focus().toggleBold().run(), isActive: () => editor.value?.isActive('bold') ?? false },
  { icon: 'pi-italic', title: 'Italic', action: () => editor.value?.chain().focus().toggleItalic().run(), isActive: () => editor.value?.isActive('italic') ?? false },
  { icon: 'pi-underline', title: 'Underline', action: () => editor.value?.chain().focus().toggleUnderline().run(), isActive: () => editor.value?.isActive('underline') ?? false },
  { icon: 'pi-strikethrough', title: 'Strikethrough', action: () => editor.value?.chain().focus().toggleStrike().run(), isActive: () => editor.value?.isActive('strike') ?? false },
])

const headingButtons = computed<ToolbarButton[]>(() => [
  { icon: '', title: 'H1', action: () => editor.value?.chain().focus().toggleHeading({ level: 1 }).run(), isActive: () => editor.value?.isActive('heading', { level: 1 }) ?? false },
  { icon: '', title: 'H2', action: () => editor.value?.chain().focus().toggleHeading({ level: 2 }).run(), isActive: () => editor.value?.isActive('heading', { level: 2 }) ?? false },
  { icon: '', title: 'H3', action: () => editor.value?.chain().focus().toggleHeading({ level: 3 }).run(), isActive: () => editor.value?.isActive('heading', { level: 3 }) ?? false },
])

const listButtons = computed<ToolbarButton[]>(() => [
  { icon: 'pi-list', title: 'Bullet List', action: () => editor.value?.chain().focus().toggleBulletList().run(), isActive: () => editor.value?.isActive('bulletList') ?? false },
  { icon: 'pi-sort-numeric-up', title: 'Ordered List', action: () => editor.value?.chain().focus().toggleOrderedList().run(), isActive: () => editor.value?.isActive('orderedList') ?? false },
  { icon: 'pi-check-square', title: 'Task List', action: () => editor.value?.chain().focus().toggleTaskList().run(), isActive: () => editor.value?.isActive('taskList') ?? false },
])

const alignButtons = computed<ToolbarButton[]>(() => [
  { icon: 'pi-align-right', title: 'Align Right', action: () => editor.value?.chain().focus().setTextAlign('right').run(), isActive: () => editor.value?.isActive({ textAlign: 'right' }) ?? false },
  { icon: 'pi-align-center', title: 'Align Center', action: () => editor.value?.chain().focus().setTextAlign('center').run(), isActive: () => editor.value?.isActive({ textAlign: 'center' }) ?? false },
  { icon: 'pi-align-left', title: 'Align Left', action: () => editor.value?.chain().focus().setTextAlign('left').run(), isActive: () => editor.value?.isActive({ textAlign: 'left' }) ?? false },
  { icon: 'pi-align-justify', title: 'Justify', action: () => editor.value?.chain().focus().setTextAlign('justify').run(), isActive: () => editor.value?.isActive({ textAlign: 'justify' }) ?? false },
])

const insertButtons = computed<ToolbarButton[]>(() => [
  { icon: 'pi-table', title: 'Insert Table', action: insertTable },
  { icon: 'pi-link', title: 'Link', action: setLink, isActive: () => editor.value?.isActive('link') ?? false },
  { icon: 'pi-image', title: 'Image', action: insertImage },
])

const historyButtons = computed<ToolbarButton[]>(() => [
  { icon: 'pi-undo', title: 'Undo', action: () => editor.value?.chain().focus().undo().run(), disabled: () => !editor.value?.can().undo() },
  { icon: 'pi-refresh', title: 'Redo', action: () => editor.value?.chain().focus().redo().run(), disabled: () => !editor.value?.can().redo() },
])

/* Table-specific actions (shown when cursor is inside a table) */
const isInTable = computed(() => editor.value?.isActive('table') ?? false)
</script>

<template>
  <div
    :class="editorContainerClass"
    :dir="isRtl ? 'rtl' : 'ltr'"
  >
    <!-- Toolbar -->
    <div
      v-if="editable"
      class="flex flex-wrap items-center gap-0.5 border-b border-surface-dim bg-surface-muted/50 px-2 py-1.5"
      :class="compact ? 'gap-0' : 'gap-0.5'"
    >
      <!-- Text Format -->
      <div class="flex items-center gap-0.5">
        <button
          v-for="btn in textFormatButtons"
          :key="btn.title"
          type="button"
          class="rounded p-1.5 text-sm transition-colors"
          :class="btn.isActive?.() ? 'bg-primary/15 text-primary' : 'text-tertiary hover:bg-surface-muted hover:text-secondary'"
          :title="btn.title"
          @click="btn.action()"
        >
          <i class="pi" :class="btn.icon" />
        </button>
      </div>

      <span class="mx-1 h-5 w-px bg-surface-dim" />

      <!-- Headings -->
      <div class="flex items-center gap-0.5">
        <button
          v-for="btn in headingButtons"
          :key="btn.title"
          type="button"
          class="rounded px-1.5 py-1 text-xs font-bold transition-colors"
          :class="btn.isActive?.() ? 'bg-primary/15 text-primary' : 'text-tertiary hover:bg-surface-muted hover:text-secondary'"
          :title="btn.title"
          @click="btn.action()"
        >
          {{ btn.title }}
        </button>
      </div>

      <span class="mx-1 h-5 w-px bg-surface-dim" />

      <!-- Lists -->
      <div class="flex items-center gap-0.5">
        <button
          v-for="btn in listButtons"
          :key="btn.title"
          type="button"
          class="rounded p-1.5 text-sm transition-colors"
          :class="btn.isActive?.() ? 'bg-primary/15 text-primary' : 'text-tertiary hover:bg-surface-muted hover:text-secondary'"
          :title="btn.title"
          @click="btn.action()"
        >
          <i class="pi" :class="btn.icon" />
        </button>
      </div>

      <span class="mx-1 h-5 w-px bg-surface-dim" />

      <!-- Alignment -->
      <div class="flex items-center gap-0.5">
        <button
          v-for="btn in alignButtons"
          :key="btn.title"
          type="button"
          class="rounded p-1.5 text-sm transition-colors"
          :class="btn.isActive?.() ? 'bg-primary/15 text-primary' : 'text-tertiary hover:bg-surface-muted hover:text-secondary'"
          :title="btn.title"
          @click="btn.action()"
        >
          <i class="pi" :class="btn.icon" />
        </button>
      </div>

      <span class="mx-1 h-5 w-px bg-surface-dim" />

      <!-- Insert -->
      <div class="flex items-center gap-0.5">
        <button
          v-for="btn in insertButtons"
          :key="btn.title"
          type="button"
          class="rounded p-1.5 text-sm transition-colors"
          :class="btn.isActive?.() ? 'bg-primary/15 text-primary' : 'text-tertiary hover:bg-surface-muted hover:text-secondary'"
          :title="btn.title"
          @click="btn.action()"
        >
          <i class="pi" :class="btn.icon" />
        </button>
      </div>

      <!-- Table actions (visible when inside table) -->
      <template v-if="isInTable">
        <span class="mx-1 h-5 w-px bg-surface-dim" />
        <div class="flex items-center gap-0.5">
          <button
            type="button"
            class="rounded px-2 py-1 text-xs text-tertiary hover:bg-surface-muted hover:text-secondary"
            title="Add Row After"
            @click="editor?.chain().focus().addRowAfter().run()"
          >
            <i class="pi pi-plus text-[10px]" /> Row
          </button>
          <button
            type="button"
            class="rounded px-2 py-1 text-xs text-tertiary hover:bg-surface-muted hover:text-secondary"
            title="Add Column After"
            @click="editor?.chain().focus().addColumnAfter().run()"
          >
            <i class="pi pi-plus text-[10px]" /> Col
          </button>
          <button
            type="button"
            class="rounded px-2 py-1 text-xs text-red-500 hover:bg-red-50"
            title="Delete Row"
            @click="editor?.chain().focus().deleteRow().run()"
          >
            <i class="pi pi-minus text-[10px]" /> Row
          </button>
          <button
            type="button"
            class="rounded px-2 py-1 text-xs text-red-500 hover:bg-red-50"
            title="Delete Column"
            @click="editor?.chain().focus().deleteColumn().run()"
          >
            <i class="pi pi-minus text-[10px]" /> Col
          </button>
          <button
            type="button"
            class="rounded px-2 py-1 text-xs text-red-500 hover:bg-red-50"
            title="Delete Table"
            @click="editor?.chain().focus().deleteTable().run()"
          >
            <i class="pi pi-trash text-[10px]" />
          </button>
        </div>
      </template>

      <span class="mx-1 h-5 w-px bg-surface-dim" />

      <!-- Color -->
      <div class="flex items-center gap-0.5">
        <label class="relative cursor-pointer rounded p-1.5 text-sm text-tertiary hover:bg-surface-muted hover:text-secondary" title="Text Color">
          <i class="pi pi-palette" />
          <input
            type="color"
            class="absolute inset-0 h-0 w-0 opacity-0"
            @input="(e) => editor?.chain().focus().setColor((e.target as HTMLInputElement).value).run()"
          />
        </label>
        <label class="relative cursor-pointer rounded p-1.5 text-sm text-tertiary hover:bg-surface-muted hover:text-secondary" title="Highlight">
          <i class="pi pi-pencil" />
          <input
            type="color"
            class="absolute inset-0 h-0 w-0 opacity-0"
            value="#FFFF00"
            @input="(e) => editor?.chain().focus().toggleHighlight({ color: (e.target as HTMLInputElement).value }).run()"
          />
        </label>
      </div>

      <span class="mx-1 h-5 w-px bg-surface-dim" />

      <!-- Undo/Redo -->
      <div class="flex items-center gap-0.5">
        <button
          v-for="btn in historyButtons"
          :key="btn.title"
          type="button"
          class="rounded p-1.5 text-sm transition-colors"
          :class="btn.disabled?.() ? 'cursor-not-allowed text-surface-dim' : 'text-tertiary hover:bg-surface-muted hover:text-secondary'"
          :title="btn.title"
          :disabled="btn.disabled?.()"
          @click="btn.action()"
        >
          <i class="pi" :class="btn.icon" />
        </button>
      </div>

      <!-- Blockquote & Code Block & Horizontal Rule -->
      <span class="mx-1 h-5 w-px bg-surface-dim" />
      <div class="flex items-center gap-0.5">
        <button
          type="button"
          class="rounded p-1.5 text-sm transition-colors"
          :class="editor?.isActive('blockquote') ? 'bg-primary/15 text-primary' : 'text-tertiary hover:bg-surface-muted hover:text-secondary'"
          title="Blockquote"
          @click="editor?.chain().focus().toggleBlockquote().run()"
        >
          <i class="pi pi-comment" />
        </button>
        <button
          type="button"
          class="rounded p-1.5 text-sm transition-colors"
          :class="editor?.isActive('codeBlock') ? 'bg-primary/15 text-primary' : 'text-tertiary hover:bg-surface-muted hover:text-secondary'"
          title="Code Block"
          @click="editor?.chain().focus().toggleCodeBlock().run()"
        >
          <i class="pi pi-code" />
        </button>
        <button
          type="button"
          class="rounded p-1.5 text-sm text-tertiary hover:bg-surface-muted hover:text-secondary"
          title="Horizontal Rule"
          @click="editor?.chain().focus().setHorizontalRule().run()"
        >
          <i class="pi pi-minus" />
        </button>
      </div>
    </div>

    <!-- Editor Content -->
    <EditorContent
      :editor="editor"
      class="tiptap-content"
      :style="{ minHeight: props.minHeight, maxHeight: props.maxHeight }"
    />
  </div>
</template>

<style>
/* ── TipTap Editor Styles ────────────────────────────── */
.tiptap-content .tiptap {
  padding: 1rem;
  outline: none;
  overflow-y: auto;
  font-size: 0.875rem;
  line-height: 1.75;
  color: var(--color-secondary, #1f2937);
}

.tiptap-content .tiptap p.is-editor-empty:first-child::before {
  content: attr(data-placeholder);
  float: right;
  color: #9ca3af;
  pointer-events: none;
  height: 0;
}

[dir="ltr"] .tiptap-content .tiptap p.is-editor-empty:first-child::before {
  float: left;
}

/* Headings */
.tiptap-content .tiptap h1 { font-size: 1.5rem; font-weight: 700; margin: 1rem 0 0.5rem; }
.tiptap-content .tiptap h2 { font-size: 1.25rem; font-weight: 700; margin: 0.75rem 0 0.5rem; }
.tiptap-content .tiptap h3 { font-size: 1.1rem; font-weight: 600; margin: 0.5rem 0 0.25rem; }

/* Lists */
.tiptap-content .tiptap ul { list-style-type: disc; padding-inline-start: 1.5rem; margin: 0.5rem 0; }
.tiptap-content .tiptap ol { list-style-type: decimal; padding-inline-start: 1.5rem; margin: 0.5rem 0; }
.tiptap-content .tiptap li { margin: 0.25rem 0; }

/* Task List */
.tiptap-content .tiptap ul[data-type="taskList"] {
  list-style: none;
  padding-inline-start: 0;
}
.tiptap-content .tiptap ul[data-type="taskList"] li {
  display: flex;
  align-items: flex-start;
  gap: 0.5rem;
}
.tiptap-content .tiptap ul[data-type="taskList"] li > label {
  flex-shrink: 0;
  margin-top: 0.25rem;
}

/* Table */
.tiptap-content .tiptap table {
  border-collapse: collapse;
  width: 100%;
  margin: 0.75rem 0;
  table-layout: fixed;
  overflow: hidden;
}
.tiptap-content .tiptap table td,
.tiptap-content .tiptap table th {
  border: 1px solid #d1d5db;
  padding: 0.5rem 0.75rem;
  vertical-align: top;
  min-width: 80px;
  position: relative;
}
.tiptap-content .tiptap table th {
  background-color: #f3f4f6;
  font-weight: 600;
}
.tiptap-content .tiptap table .selectedCell {
  background-color: #dbeafe;
}

/* Blockquote */
.tiptap-content .tiptap blockquote {
  border-inline-start: 4px solid #6366f1;
  padding-inline-start: 1rem;
  margin: 0.75rem 0;
  color: #4b5563;
  font-style: italic;
}

/* Code Block */
.tiptap-content .tiptap pre {
  background: #1f2937;
  color: #e5e7eb;
  padding: 0.75rem 1rem;
  border-radius: 0.5rem;
  font-family: monospace;
  font-size: 0.8rem;
  overflow-x: auto;
  margin: 0.5rem 0;
}

/* Horizontal Rule */
.tiptap-content .tiptap hr {
  border: none;
  border-top: 2px solid #e5e7eb;
  margin: 1rem 0;
}

/* Link */
.tiptap-content .tiptap a {
  color: #2563eb;
  text-decoration: underline;
  cursor: pointer;
}

/* Image */
.tiptap-content .tiptap img {
  max-width: 100%;
  height: auto;
  border-radius: 0.375rem;
  margin: 0.5rem 0;
}

/* Highlight */
.tiptap-content .tiptap mark {
  border-radius: 0.125rem;
  padding: 0.125rem 0;
}
</style>
