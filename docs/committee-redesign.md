# Committee Management System Redesign

## 1. Committee Scope Model (ScopeType)

New enum `CommitteeScopeType`:
- `Comprehensive = 1` — All phases, all competitions
- `SpecificPhasesAllCompetitions = 2` — Specific phases, all competitions  
- `SpecificPhasesSpecificCompetitions = 3` — Specific phases, specific competitions

## 2. Domain Changes

### Committee Entity Changes:
- Remove `CompetitionId` (single FK) — replaced by M:N `CommitteeCompetition` table
- Add `ScopeType` (CommitteeScopeType enum)
- Keep `ActiveFromPhase` / `ActiveToPhase` for phase scoping
- When `ScopeType = Comprehensive`, phases are null (all phases)
- When `ScopeType = SpecificPhasesAllCompetitions`, phases are set, no competition links
- When `ScopeType = SpecificPhasesSpecificCompetitions`, phases are set + competition links

### New Entity: CommitteeCompetition (M:N)
- `CommitteeId`, `CompetitionId`, `AssignedAt`, `AssignedBy`

### CommitteeMember Changes:
- Remove `UserFullName` (cached) — fetch dynamically from ApplicationUser
- Add FK to ApplicationUser (validated on add)
- Keep `CommitteeMemberRole` (Chair/Member/Secretary)

### AddMember Validation:
1. User must exist in ApplicationUser (same tenant)
2. User must be active
3. User's platform SystemRole must be compatible with committee role
4. Conflict of interest rules still apply

## 3. Role Compatibility Matrix

| CommitteeMemberRole | Allowed SystemRoles |
|---------------------|---------------------|
| Chair | Owner, Admin, SectorRep |
| Member | Owner, Admin, SectorRep, FinancialController, Member |
| Secretary | Admin, SectorRep, Member |

## 4. API Changes

### New Endpoint: Search eligible users for committee
`GET /api/v1/committees/{id}/eligible-users?search=&role=`
- Returns users from same tenant who are active and role-compatible
- Excludes users already active members of this committee

### Updated: AddCommitteeMember
- Remove `userFullName` from request (fetched from DB)
- Validate user exists and is active
- Validate role compatibility

### Updated: CreateCommittee
- Add `scopeType` field
- Add `competitionIds` (array) for SpecificPhasesSpecificCompetitions
- Validation rules per scope type

## 5. Frontend Changes

### Add Member Dialog:
- Replace manual userId/userFullName inputs with searchable user dropdown
- Fetch from `/api/v1/committees/{id}/eligible-users`
- Show user name, email, and platform role
- Auto-filter by committee role compatibility

### Create Committee Dialog:
- Add scope type selector (3 radio options)
- Conditionally show phase selectors and competition multi-select
- Dynamic validation based on scope type
