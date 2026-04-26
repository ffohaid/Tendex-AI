export interface TaskNavigationInput {
  actionUrl?: string | null
  type?: string | null
}

export function resolveTaskActionUrl(task: TaskNavigationInput): string | null {
  if (!task.actionUrl) {
    return null
  }

  const targetUrl = task.actionUrl

  if (targetUrl.startsWith('/competitions/')) {
    const id = targetUrl.replace('/competitions/', '').split('?')[0]

    if (task.type === 'approve_request' || task.type === 'approval') {
      return `/approvals?competitionId=${id}`
    }

    if (task.type === 'evaluate_offer') {
      return `/evaluation/offers/${id}`
    }

    return `/approvals?competitionId=${id}`
  }

  if (
    targetUrl.startsWith('/approvals')
    || targetUrl.startsWith('/rfp/booklet-editor/')
    || targetUrl.startsWith('/evaluation/')
    || targetUrl.startsWith('/inquiries/')
  ) {
    return targetUrl
  }

  return targetUrl
}
