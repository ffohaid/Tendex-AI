# Workflow Role-Based Booklet Approval Fix Report

## Overview

This task addressed a critical defect in the **booklet approval workflow** where approval execution and task visibility were not working correctly according to the configured **role-based workflow**. The target behavior was that booklet approval should be driven dynamically by the configured workflow roles, not by a fixed user, while also enforcing the expected lifecycle transitions for submission, review, approval, rejection, and post-decision editability.

## Root Cause Analysis

The issue was caused by a combination of inconsistencies between the frontend submission path, workflow authorization, task-center matching logic, and backend editability rules.

| Area | Root cause | Impact |
|---|---|---|
| Booklet submission path | The frontend editor initiated approval workflow steps directly instead of always using the unified status transition path. | The lifecycle could bypass the canonical submission behavior and create inconsistencies in approval initiation. |
| Approval authorization | Approval and rejection endpoints depended on permissive or narrow authorization paths instead of resolving the current user dynamically from actual assigned roles and committee memberships. | Users with the correct workflow role could fail authorization or behave inconsistently. |
| Task-center visibility | Role matching and committee-role matching needed to be aligned with the same source of truth used by approval execution. | Approval tasks could appear inconsistently or not map exactly to the approval logic. |
| Rejection lifecycle | Rejecting a booklet approval step did not reliably return the booklet to the expected editable preparation state. | Rejected booklets could remain in a non-editable state or in the wrong lifecycle state. |
| Editability guardrails | Backend editability and auto-save rules still allowed states that should be blocked after submission/approval. | Pending or approved booklets could remain modifiable in ways that break workflow integrity. |

## Implemented Fixes

The fix was implemented as a coordinated backend and frontend correction focused on maintaining a **single source of truth** for status transitions, approval authorization, and task visibility.

| Component | Change implemented | Purpose |
|---|---|---|
| `ApprovalActorResolver.cs` | Added a shared resolver that maps identity role claims and committee memberships to workflow system roles and workflow committee roles. | Centralizes dynamic approval-actor resolution and removes duplicated interpretation logic. |
| `ApprovalWorkflowService.cs` | Added role-identifier-aware approve/reject overloads, enforced authorization using resolved role sets, and added controlled post-rejection status reversion. | Ensures approval execution is truly role-based and rejection returns the booklet to the correct stage. |
| `IApprovalWorkflowService.cs` | Extended the service contract with dynamic role-aware approval execution overloads. | Keeps service contracts aligned across layers without hidden coupling. |
| `ApprovalWorkflowEndpoints.cs` | Extracts current role claims from the authenticated user and passes them to the workflow service for approve/reject actions. | Makes runtime authorization based on the actual logged-in actor rather than a hardcoded assumption. |
| `GetPendingTasksQueryHandler.cs` | Reused the shared resolver for system-role and committee-role matching inside the task center. | Aligns task visibility with the same authorization rules used by actual approval actions. |
| `BookletEditorView.vue` | Replaced direct workflow initiation with the unified `submitRfpForApproval()` path. | Guarantees that booklet submission follows the canonical lifecycle path and auto-creates the approval workflow correctly. |
| `Competition.cs` | Introduced a central editability helper and tightened editable states so pending approval is no longer treated as editable. | Prevents silent workflow violations after submission. |
| `AutoSaveCompetitionCommandHandler.cs` | Restricted auto-save to editable draft/preparation/rejected states only. | Blocks unintended persistence in non-editable workflow states. |
| `CompetitionStateMachine.cs` | Allowed controlled transition from `PendingApproval` back to `UnderPreparation`. | Enables correct rework behavior after rejection. |

## Functional Outcome

After these changes, the booklet approval flow now behaves as follows.

| Scenario | Result after fix |
|---|---|
| User submits a completed booklet | Submission now goes through the canonical status transition path and moves the booklet into **PendingApproval**. |
| Workflow task generation | Approval workflow creation remains tied to the backend status-change handler, so the runtime workflow is generated from the configured transition. |
| Task-center visibility | Users with matching workflow roles and committee memberships are resolved dynamically and can see the relevant approval task in the task center. |
| Opening approval task | Task routing continues to point to the booklet editor with step context so the approver can review in read-only approval mode. |
| Approving booklet | Only actors matching the required workflow role set can approve the current actionable step. |
| Rejecting booklet | Rejection reverts the booklet to **UnderPreparation** for rework in the booklet-approval flow. |
| Editing after approval/submission | Pending-approval and approved booklets are no longer treated as editable in the tightened backend guards. |
| Resubmission after rejection | Rejected/reworked booklet flow remains editable and can be resubmitted through the unified submission path. |

## Files Modified

| File | Change summary |
|---|---|
| `backend/src/TendexAI.Application/Features/Workflow/Services/ApprovalActorResolver.cs` | New shared dynamic role and committee-role resolver. |
| `backend/src/TendexAI.Application/Features/Workflow/Services/IApprovalWorkflowService.cs` | Added dynamic role-aware approval contract overloads. |
| `backend/src/TendexAI.Application/Features/Workflow/Services/ApprovalWorkflowService.cs` | Added role-aware approve/reject authorization and rejection reversion logic. |
| `backend/src/TendexAI.API/Endpoints/Workflow/ApprovalWorkflowEndpoints.cs` | Forwarded authenticated role claims into approval service calls. |
| `backend/src/TendexAI.Application/Features/Dashboard/Queries/GetPendingTasks/GetPendingTasksQueryHandler.cs` | Unified task visibility matching with the shared approval-actor resolver. |
| `backend/src/TendexAI.Domain/Entities/Rfp/Competition.cs` | Centralized editable-state rules and excluded pending approval from editable states. |
| `backend/src/TendexAI.Application/Features/Rfp/Commands/AutoSaveCompetition/AutoSaveCompetitionCommandHandler.cs` | Restricted auto-save to editable stages only. |
| `backend/src/TendexAI.Domain/StateMachine/CompetitionStateMachine.cs` | Enabled return to `UnderPreparation` from `PendingApproval`. |
| `frontend/src/views/rfp/BookletEditorView.vue` | Replaced manual workflow initiation with unified booklet submission flow. |

## Verification Performed

The verification focused on both structural correctness and executable safety checks available in the current environment.

| Verification item | Result |
|---|---|
| Frontend production build | **Passed** using `pnpm build`. |
| Frontend submission path check | Confirmed booklet editor now calls `submitRfpForApproval()` rather than initiating workflow directly. |
| Endpoint authorization wiring | Confirmed approval and rejection endpoints now pass authenticated role claims into role-aware workflow service overloads. |
| Task-center role alignment | Confirmed task-center matching uses the same shared resolver used by the approval service. |
| Pending-approval edit lock | Confirmed auto-save/editability guards no longer treat `PendingApproval` as editable. |
| Rejection transition path | Confirmed state machine and workflow service now support returning booklet approval rejections to `UnderPreparation`. |
| Backend full build | Not executable in the current sandbox because the .NET SDK is not available (`dotnet: command not found`). Frontend and static integration checks were completed instead. |

## Notes

The implemented fix intentionally strengthens the workflow around the **booklet approval stage** without changing unrelated lifecycle behavior outside the touched approval path. The authorization and task-center logic now share the same dynamic resolution model, which reduces drift between “who sees the task” and “who can actually execute the approval action.”

## Recommended Next Step

The next operational step should be a **live end-to-end verification** on a staging or production-like environment using at least two real users with different roles: one booklet creator and one approver whose authority comes from the configured workflow role. That verification should cover submission, task appearance, opening from task center, approval, rejection, return to preparation, editability after rejection, and re-submission.
