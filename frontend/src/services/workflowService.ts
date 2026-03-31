/**
 * Approval Workflow API Service.
 *
 * Provides methods to interact with the backend Approval Workflow APIs.
 * Maps to ApprovalWorkflowEndpoints.cs — /api/v1/approval-workflows
 * and /api/v1/workflow-definitions.
 *
 * All data is fetched dynamically — no mock/hardcoded arrays allowed.
 */
import { httpGet, httpPost, httpPut } from '@/services/http'

/* ------------------------------------------------------------------ */
/*  Types                                                              */
/* ------------------------------------------------------------------ */

export interface ApprovalStepDetail {
  stepId: string
  stepOrder: number
  stepNameAr: string
  stepNameEn: string
  requiredRole: number
  requiredCommitteeRole: number
  status: ApprovalStepStatus
  completedByUserId: string | null
  completedAt: string | null
  comment: string | null
  slaDeadline: string | null
  isSlaExceeded: boolean
}

export const ApprovalStepStatus = {
  Pending: 0,
  InProgress: 1,
  Approved: 2,
  Rejected: 3,
  Skipped: 4,
} as const

export type ApprovalStepStatus =
  (typeof ApprovalStepStatus)[keyof typeof ApprovalStepStatus]

export interface ApprovalWorkflowStatusResult {
  hasWorkflow: boolean
  totalSteps: number
  completedSteps: number
  currentStepOrder: number
  isCompleted: boolean
  isRejected: boolean
  steps: ApprovalStepDetail[]
}

export interface ApprovalWorkflowInitiationResult {
  workflowDefinitionId: string | null
  totalSteps: number
  stepIds: string[]
}

export interface ApprovalActionResult {
  stepId: string
  competitionId: string
  isWorkflowCompleted: boolean
  fromStatus: number
  toStatus: number
}

export interface CompetitionWorkflowsResponse {
  bookletSubmission: ApprovalWorkflowStatusResult
  bookletApproval: ApprovalWorkflowStatusResult
}

export interface WorkflowStepDefinitionDto {
  id: string
  stepOrder: number
  requiredSystemRole: number
  requiredCommitteeRole: number
  stepNameAr: string
  stepNameEn: string
  slaHours: number | null
  isConditional: boolean
  conditionExpression: string | null
  isActive: boolean
}

export interface WorkflowDefinitionDto {
  id: string
  nameAr: string
  nameEn: string
  descriptionAr: string | null
  descriptionEn: string | null
  transitionFrom: number
  transitionTo: number
  isActive: boolean
  version: number
  steps: WorkflowStepDefinitionDto[]
}

/* ------------------------------------------------------------------ */
/*  Request Types                                                      */
/* ------------------------------------------------------------------ */

export interface InitiateWorkflowRequest {
  competitionId: string
  fromStatus: number
  toStatus: number
}

export interface ApproveStepRequest {
  comment?: string | null
}

export interface RejectStepRequest {
  reason: string
}

export interface CreateWorkflowStepRequest {
  stepOrder: number
  requiredSystemRole: number
  requiredCommitteeRole: number
  stepNameAr: string
  stepNameEn: string
  slaHours?: number | null
  isConditional?: boolean
  conditionExpression?: string | null
}

export interface CreateWorkflowDefinitionRequest {
  nameAr: string
  nameEn: string
  descriptionAr?: string | null
  descriptionEn?: string | null
  transitionFrom: number
  transitionTo: number
  steps: CreateWorkflowStepRequest[]
}

export interface UpdateWorkflowDefinitionRequest {
  nameAr: string
  nameEn: string
  descriptionAr?: string | null
  descriptionEn?: string | null
  isActive?: boolean | null
}

/* ------------------------------------------------------------------ */
/*  Workflow Execution APIs                                             */
/* ------------------------------------------------------------------ */

const WORKFLOW_BASE = '/v1/approval-workflows'

/**
 * Initiate an approval workflow for a competition phase transition.
 */
export async function initiateWorkflow(
  request: InitiateWorkflowRequest,
): Promise<ApprovalWorkflowInitiationResult> {
  return httpPost<ApprovalWorkflowInitiationResult>(
    `${WORKFLOW_BASE}/initiate`,
    request,
  )
}

/**
 * Approve a specific step in the approval workflow.
 */
export async function approveStep(
  stepId: string,
  request: ApproveStepRequest = {},
): Promise<ApprovalActionResult> {
  return httpPost<ApprovalActionResult>(
    `${WORKFLOW_BASE}/steps/${stepId}/approve`,
    request,
  )
}

/**
 * Reject a specific step in the approval workflow.
 */
export async function rejectStep(
  stepId: string,
  request: RejectStepRequest,
): Promise<ApprovalActionResult> {
  return httpPost<ApprovalActionResult>(
    `${WORKFLOW_BASE}/steps/${stepId}/reject`,
    request,
  )
}

/**
 * Get the current status of an approval workflow for a competition transition.
 */
export async function getWorkflowStatus(
  competitionId: string,
  fromStatus: number,
  toStatus: number,
): Promise<ApprovalWorkflowStatusResult> {
  const params = new URLSearchParams({
    competitionId,
    fromStatus: String(fromStatus),
    toStatus: String(toStatus),
  })
  return httpGet<ApprovalWorkflowStatusResult>(
    `${WORKFLOW_BASE}/status?${params.toString()}`,
  )
}

/**
 * Get all approval workflow steps for a competition.
 */
export async function getCompetitionWorkflows(
  competitionId: string,
): Promise<CompetitionWorkflowsResponse> {
  return httpGet<CompetitionWorkflowsResponse>(
    `${WORKFLOW_BASE}/competition/${competitionId}`,
  )
}

/* ------------------------------------------------------------------ */
/*  Workflow Definition Management APIs (Admin)                        */
/* ------------------------------------------------------------------ */

const DEFINITION_BASE = '/v1/workflow-definitions'

/**
 * Get all workflow definitions for the current tenant.
 */
export async function getWorkflowDefinitions(): Promise<
  WorkflowDefinitionDto[]
> {
  return httpGet<WorkflowDefinitionDto[]>(DEFINITION_BASE)
}

/**
 * Get a workflow definition with its steps.
 */
export async function getWorkflowDefinitionById(
  id: string,
): Promise<WorkflowDefinitionDto> {
  return httpGet<WorkflowDefinitionDto>(`${DEFINITION_BASE}/${id}`)
}

/**
 * Create a new workflow definition.
 */
export async function createWorkflowDefinition(
  request: CreateWorkflowDefinitionRequest,
): Promise<{ id: string }> {
  return httpPost<{ id: string }>(DEFINITION_BASE, request)
}

/**
 * Update an existing workflow definition.
 */
export async function updateWorkflowDefinition(
  id: string,
  request: UpdateWorkflowDefinitionRequest,
): Promise<{ id: string; version: number }> {
  return httpPut<{ id: string; version: number }>(
    `${DEFINITION_BASE}/${id}`,
    request,
  )
}

/**
 * Seed default workflow definitions for the current tenant.
 */
export async function seedDefaultWorkflows(): Promise<{
  message: string
  count: number
}> {
  return httpPost<{ message: string; count: number }>(
    `${DEFINITION_BASE}/seed-defaults`,
  )
}
