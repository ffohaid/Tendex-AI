<script setup lang="ts">
import { computed } from 'vue'

export interface OfficialBookletBlock {
  id: string
  sortOrder: number
  html: string
  colorType?: 'fixed' | 'editable' | 'example' | 'guidance'
  isHeading?: boolean
}

export interface OfficialBookletSection {
  id: string
  titleAr: string
  sortOrder: number
  isMainSection: boolean
  blocks: OfficialBookletBlock[]
}

interface OfficialBookletMeta {
  projectNameAr: string
  projectNameEn?: string
  templateNameAr?: string
  referenceNumber?: string
  issueDate?: string
  administrationName?: string
  organizationName?: string
  organizationNameEn?: string
  logoUrl?: string | null
  versionLabel?: string
}

interface SectionGroup {
  id: string
  titleAr: string
  sortOrder: number
  mainSection: OfficialBookletSection | null
  items: OfficialBookletSection[]
}

const props = withDefaults(defineProps<{
  meta: OfficialBookletMeta
  sections: OfficialBookletSection[]
  includeGuidance?: boolean
  includeExampleBlocks?: boolean
  showCover?: boolean
  showTableOfContents?: boolean
}>(), {
  includeGuidance: false,
  includeExampleBlocks: true,
  showCover: true,
  showTableOfContents: true,
})

const filteredSections = computed<OfficialBookletSection[]>(() => {
  return [...props.sections]
    .sort((a, b) => a.sortOrder - b.sortOrder)
    .map(section => ({
      ...section,
      blocks: [...section.blocks]
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .filter(block => {
          if (!props.includeGuidance && block.colorType === 'guidance') return false
          if (!props.includeExampleBlocks && block.colorType === 'example') return false
          return true
        }),
    }))
})

const groupedSections = computed<SectionGroup[]>(() => {
  const groups: SectionGroup[] = []
  let currentGroup: SectionGroup | null = null

  for (const section of filteredSections.value) {
    if (section.isMainSection) {
      currentGroup = {
        id: section.id,
        titleAr: section.titleAr,
        sortOrder: section.sortOrder,
        mainSection: section,
        items: [],
      }
      groups.push(currentGroup)
      continue
    }

    if (!currentGroup) {
      currentGroup = {
        id: `group-${section.id}`,
        titleAr: 'الأقسام',
        sortOrder: section.sortOrder,
        mainSection: null,
        items: [],
      }
      groups.push(currentGroup)
    }

    currentGroup.items.push(section)
  }

  return groups
})

const printableIssueDate = computed(() => props.meta.issueDate || '-')
const printableReferenceNumber = computed(() => props.meta.referenceNumber || '-')
const printableAdministrationName = computed(() => props.meta.administrationName || '-')
const printableOrganizationName = computed(() => props.meta.organizationName || 'الجهة الحكومية')
const printableOrganizationNameEn = computed(() => props.meta.organizationNameEn || '')
const printableVersionLabel = computed(() => props.meta.versionLabel || 'الأولى')
const printableTemplateName = computed(() => props.meta.templateNameAr || 'كراسة الشروط والمواصفات')

function stripLeadingSectionNumber(title: string): string {
  return title
    .replace(/^\s*[0-9٠-٩]+\s*[-–—.:،]?\s*/u, '')
    .replace(/^\s*القسم\s+[0-9٠-٩A-Za-z\u0621-\u064A]+\s*[:：-]?\s*/u, '')
    .trim()
}

function renderSectionTitle(section: OfficialBookletSection): string {
  if (section.isMainSection) {
    return section.titleAr
  }

  const cleanTitle = stripLeadingSectionNumber(section.titleAr)
  return `${section.sortOrder} - ${cleanTitle}`
}

function hasRenderableBlocks(section: OfficialBookletSection | null): boolean {
  if (!section) return false
  return section.blocks.some(block => block.html?.trim())
}
</script>

<template>
  <div class="official-booklet-document mx-auto max-w-[210mm] bg-white text-secondary-900" dir="rtl">
    <section v-if="showCover" class="booklet-page booklet-cover">
      <div class="cover-brand-row">
        <div class="brand-chip brand-chip-start">
          <div class="brand-chip__title">{{ printableTemplateName }}</div>
          <div class="brand-chip__subtitle">كراسة الشروط والمواصفات</div>
        </div>
        <div class="brand-chip brand-chip-end">
          <div class="brand-chip__title">{{ printableOrganizationName }}</div>
          <div class="brand-chip__subtitle">{{ printableOrganizationNameEn || printableAdministrationName }}</div>
        </div>
      </div>

      <div class="cover-emblem">
        <img
          v-if="meta.logoUrl"
          :src="meta.logoUrl"
          :alt="printableOrganizationName"
          class="mx-auto max-h-24 w-auto object-contain"
        />
        <span v-else>{{ printableOrganizationName }}</span>
      </div>

      <div class="cover-title-wrap">
        <h1 class="cover-title">نموذج كراسة الشروط والمواصفات</h1>
        <h2 class="cover-subtitle">{{ printableTemplateName }}</h2>
      </div>

      <p class="cover-approval-text">
        المعتمد بموجب قرار وزير المالية رقم (1440) وتاريخ 12/4/1441هـ، والمعدل بموجب قرار وزير المالية رقم (1156) وتاريخ 17/10/1445هـ
      </p>

      <div class="cover-metadata">
        <p><span class="cover-metadata__label">اسم المنافسة:</span> {{ meta.projectNameAr || '-' }}</p>
        <p><span class="cover-metadata__label">رقم الكراسة:</span> {{ printableReferenceNumber }}</p>
        <p><span class="cover-metadata__label">تاريخ طرح الكراسة:</span> {{ printableIssueDate }}</p>
      </div>
    </section>

    <section v-if="showTableOfContents" class="booklet-page booklet-page--framed">
      <header class="page-frame-header">
        <div class="page-frame-header__meta">
          <div><strong>رقم النسخة:</strong> {{ printableVersionLabel }}</div>
          <div><strong>تاريخ الإصدار:</strong> {{ printableIssueDate }}</div>
          <div><strong>رقم الكراسة:</strong> {{ printableReferenceNumber }}</div>
        </div>
        <div class="page-frame-header__identity">
          <div v-if="meta.logoUrl" class="page-frame-header__logo-wrap">
            <img
              :src="meta.logoUrl"
              :alt="printableOrganizationName"
              class="page-frame-header__logo"
            />
          </div>
          <div class="page-frame-header__country">{{ printableOrganizationName }}</div>
          <div><strong>اسم الإدارة:</strong> {{ printableAdministrationName }}</div>
          <div><strong>اسم النموذج:</strong> {{ printableTemplateName }}</div>
        </div>
      </header>

      <div class="toc-heading">الفهرس</div>

      <div class="toc-groups">
        <section v-for="group in groupedSections" :key="group.id" class="toc-group">
          <h3 class="toc-group__title">{{ group.titleAr }}</h3>
          <ul class="toc-group__items">
            <li v-for="section in group.items" :key="section.id" class="toc-group__item">
              <span class="toc-group__item-order">{{ section.sortOrder }}</span>
              <span>{{ stripLeadingSectionNumber(section.titleAr) }}</span>
            </li>
          </ul>
        </section>
      </div>
    </section>

    <template v-for="group in groupedSections" :key="`body-${group.id}`">
      <section class="booklet-page booklet-page--framed booklet-page--body">
        <header class="page-frame-header">
          <div class="page-frame-header__meta">
            <div><strong>رقم النسخة:</strong> {{ printableVersionLabel }}</div>
            <div><strong>تاريخ الإصدار:</strong> {{ printableIssueDate }}</div>
            <div><strong>رقم الكراسة:</strong> {{ printableReferenceNumber }}</div>
          </div>
          <div class="page-frame-header__identity">
            <div v-if="meta.logoUrl" class="page-frame-header__logo-wrap">
              <img
                :src="meta.logoUrl"
                :alt="printableOrganizationName"
                class="page-frame-header__logo"
              />
            </div>
            <div class="page-frame-header__country">{{ printableOrganizationName }}</div>
            <div><strong>اسم الإدارة:</strong> {{ printableAdministrationName }}</div>
            <div><strong>اسم النموذج:</strong> {{ printableTemplateName }}</div>
          </div>
        </header>

        <div v-if="group.mainSection" class="booklet-main-section-title">
          {{ group.mainSection.titleAr }}
        </div>

        <div v-if="group.mainSection && hasRenderableBlocks(group.mainSection)" class="section-blocks">
          <div
            v-for="block in group.mainSection.blocks"
            :key="block.id"
            class="booklet-block"
            :class="{
              'booklet-block--heading': block.isHeading,
              'booklet-block--guidance': block.colorType === 'guidance',
              'booklet-block--example': block.colorType === 'example',
            }"
            v-html="block.html"
          ></div>
        </div>

        <article v-for="section in group.items" :key="section.id" class="booklet-section-article">
          <h3 class="booklet-section-title">{{ renderSectionTitle(section) }}</h3>
          <div class="section-blocks">
            <div
              v-for="block in section.blocks"
              :key="block.id"
              class="booklet-block"
              :class="{
                'booklet-block--heading': block.isHeading,
                'booklet-block--guidance': block.colorType === 'guidance',
                'booklet-block--example': block.colorType === 'example',
              }"
              v-html="block.html"
            ></div>
          </div>
        </article>
      </section>
    </template>
  </div>
</template>

<style scoped>
.official-booklet-document {
  font-family: 'Tahoma', 'Arial', sans-serif;
}

.booklet-page {
  min-height: 297mm;
  padding: 18mm 14mm;
  background: white;
}

.booklet-page + .booklet-page {
  margin-top: 1.5rem;
}

.booklet-page--framed {
  border: 1px solid rgb(226 232 240);
}

.cover-brand-row {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1rem;
}

.brand-chip {
  min-width: 190px;
}

.brand-chip-end {
  text-align: end;
}

.brand-chip__title {
  font-size: 1.9rem;
  font-weight: 800;
  color: rgb(15 23 42);
}

.brand-chip__subtitle {
  margin-top: 0.35rem;
  font-size: 0.78rem;
  color: rgb(71 85 105);
}

.cover-emblem {
  margin: 5rem auto 2rem;
  display: flex;
  height: 120px;
  width: 120px;
  align-items: center;
  justify-content: center;
  border-radius: 9999px;
  background: linear-gradient(135deg, rgb(219 234 254), rgb(59 130 246));
  color: white;
  font-size: 1.25rem;
  font-weight: 800;
  box-shadow: 0 10px 30px rgb(59 130 246 / 0.25);
}

.cover-title-wrap {
  text-align: center;
}

.cover-title {
  margin: 0;
  font-size: 2.3rem;
  font-weight: 800;
  color: black;
}

.cover-subtitle {
  margin-top: 1rem;
  font-size: 1.85rem;
  font-weight: 700;
  color: black;
}

.cover-approval-text {
  margin: 2.5rem auto 0;
  max-width: 900px;
  text-align: center;
  font-size: 1.25rem;
  line-height: 2;
  color: black;
}

.cover-metadata {
  margin-top: 3rem;
  text-align: center;
  font-size: 1.35rem;
  line-height: 2.1;
  font-weight: 700;
}

.cover-metadata__label {
  color: rgb(15 23 42);
}

.page-frame-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 2rem;
  margin-bottom: 3rem;
  font-size: 0.95rem;
  line-height: 1.8;
}

.page-frame-header__meta,
.page-frame-header__identity {
  display: flex;
  flex-direction: column;
  gap: 0.15rem;
}

.page-frame-header__country {
  font-size: 1.25rem;
  font-weight: 800;
}

.page-frame-header__identity {
  text-align: end;
}

.page-frame-header__logo-wrap {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 0.75rem;
}

.page-frame-header__logo {
  max-width: 160px;
  max-height: 56px;
  width: auto;
  height: auto;
  object-fit: contain;
}

.toc-heading {
  margin-bottom: 2rem;
  text-align: end;
  font-size: 2rem;
  font-weight: 800;
  color: black;
}

.toc-groups {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.toc-group__title {
  margin: 0 0 0.85rem;
  color: rgb(22 101 52);
  font-size: 1.6rem;
  font-weight: 800;
}

.toc-group__items {
  list-style: none;
  padding: 0;
  margin: 0;
  display: flex;
  flex-direction: column;
  gap: 0.6rem;
}

.toc-group__item {
  display: flex;
  justify-content: flex-start;
  align-items: baseline;
  gap: 0.85rem;
  font-size: 1.25rem;
  color: rgb(17 24 39);
}

.toc-group__item-order {
  min-width: 2rem;
  color: rgb(22 163 74);
  font-weight: 800;
}

.booklet-main-section-title {
  margin-bottom: 2.5rem;
  color: rgb(22 101 52);
  font-size: 1.8rem;
  font-weight: 800;
}

.booklet-section-article + .booklet-section-article {
  margin-top: 2rem;
}

.booklet-section-title {
  margin: 0 0 1rem;
  font-size: 1.35rem;
  font-weight: 800;
  color: rgb(17 24 39);
}

.section-blocks {
  display: flex;
  flex-direction: column;
  gap: 0.9rem;
}

.booklet-block {
  font-size: 1rem;
  line-height: 1.95;
  color: rgb(17 24 39);
}

.booklet-block--heading {
  font-size: 1.15rem;
  font-weight: 800;
}

.booklet-block--guidance {
  border-inline-start: 4px solid rgb(59 130 246);
  padding-inline-start: 1rem;
  color: rgb(30 64 175);
}

.booklet-block--example {
  border-inline-start: 4px solid rgb(220 38 38);
  padding-inline-start: 1rem;
}

.booklet-block :deep(p),
.booklet-block :deep(ul),
.booklet-block :deep(ol),
.booklet-block :deep(table),
.booklet-block :deep(blockquote) {
  margin-block: 0.6rem;
}

.booklet-block :deep(table) {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.96rem;
}

.booklet-block :deep(th) {
  background: rgb(51 65 85);
  color: white;
  font-weight: 700;
}

.booklet-block :deep(th),
.booklet-block :deep(td) {
  border: 1px solid rgb(203 213 225);
  padding: 0.8rem 0.9rem;
  vertical-align: top;
}

.booklet-block :deep(ul),
.booklet-block :deep(ol) {
  padding-inline-start: 1.4rem;
}

.booklet-block :deep(h1),
.booklet-block :deep(h2),
.booklet-block :deep(h3),
.booklet-block :deep(h4) {
  margin: 0.8rem 0;
  font-weight: 800;
  color: rgb(15 23 42);
}

@media print {
  .booklet-page {
    margin: 0;
    min-height: auto;
    page-break-after: always;
    break-after: page;
    box-shadow: none !important;
  }

  .booklet-page--framed {
    border: 0;
  }

  .booklet-page + .booklet-page {
    margin-top: 0;
  }
}
</style>
