export interface TaskNavigationInput {
  actionUrl?: string | null
  type?: string | null
}

export function resolveTaskActionUrl(task: TaskNavigationInput): string | null {
  if (!task.actionUrl) {
    return null
  }

  let targetUrl = task.actionUrl

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

  if (targetUrl.startsWith('/approvals')) {
    const query = targetUrl.split('?')[1] || ''
    const params = new URLSearchParams(query)
    const competitionId = params.get('competitionId')
    const stepId = params.get('stepId')

    if (competitionId) {
      const stepQuery = stepId ? `?stepId=${stepId}` : ''
      return `/rfp/${competitionId}/edit${stepQuery}`
    }
  }

  return targetUrl
}
