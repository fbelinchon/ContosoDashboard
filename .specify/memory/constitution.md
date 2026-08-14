<!-- Constitution Version Control Report
     Version Change: 0.0.0 → 1.0.0 (MAJOR - Initial establishment)
     Created: All principles established for SDD training project
     Governance: Amendment procedure, versioning policy, compliance review
-->

# ContosoDashboard Constitution

## Core Principles

### I. Spec-Driven Development (Mandatory)

All features and changes MUST be preceded by a formal specification document (spec.md). The specification MUST define: user stories, acceptance criteria, technical considerations, success metrics, and architectural impact. Specifications are written collaboratively and approved before implementation begins. No implementation proceeds without an approved spec.

### II. Clean Architecture

The codebase MUST maintain clear separation of concerns across four layers: Models (data structures), Services (business logic), Data (persistence), and Pages (user interface). Dependencies MUST flow inward—higher layers depend on lower layers, never the reverse. Each service focuses on a single responsibility and exposes clear contracts. This structure enables testing, reusability, and maintainability.

### III. Security by Design (Non-Negotiable)

All features MUST integrate authorization checks at multiple layers: middleware, page attributes, and service logic. IDOR (Insecure Direct Object Reference) vulnerabilities MUST be prevented through runtime authorization verification. Each user accesses only their authorized data. Security design MUST be reviewed as part of the spec and implementation phases. Claims-based identity and role-based access control (RBAC) are the standard patterns.

### IV. Test-First Approach

Tests MUST be written before or during implementation (TDD discipline). Unit tests verify business logic in isolation. Integration tests verify cross-layer data flow and API contracts. Test coverage targets 80%+ for critical paths (authentication, data access, business logic). Failing tests indicate gaps in understanding; their resolution is non-negotiable before merge.

### V. Observability and Diagnostics

Application behavior MUST be traceable through structured logging and debug output. Errors MUST include sufficient context for diagnosis without exposing sensitive information. Database operations and service calls MUST be logged at appropriate levels. The application MUST support offline operation and provide clear feedback on state and failures.

## Architecture Principles

### Offline-First with Cloud Migration Path

This training application implements an offline-first architecture with abstraction layers that enable seamless migration to Azure services. Current implementation uses SQL Server LocalDB for data persistence. Service interfaces are designed to support future Azure Storage, Azure SQL, and Azure Cosmos DB integration without changing feature code. All external integrations MUST be optional; the application functions correctly offline.

### Data Isolation and Multi-Tenancy Readiness

User data MUST be logically isolated by authenticated user context. Services MUST respect authorization boundaries and prevent cross-user data access at runtime. The data schema and service layer design MUST support future multi-tenancy with minimal refactoring. Entity Framework Core relationships MUST enforce business rules and maintain referential integrity.

## Development Workflow

### Feature Development Lifecycle

1. **Spec Phase**: Write spec.md with user stories, acceptance criteria, technical design, and success metrics
2. **Review Phase**: Spec undergoes technical and stakeholder review; approval gates the implementation
3. **Planning Phase**: Break spec into implementation plan (plan.md) and task list (tasks.md) with dependencies
4. **Implementation Phase**: Develop features in order of task dependencies; write tests concurrently with code
5. **Testing Phase**: Run unit and integration tests; verify acceptance criteria; security review
6. **Documentation Phase**: Update README, architecture docs, and API documentation
7. **Merge Phase**: Comprehensive review (code, tests, docs); verify governance compliance before merge to main

### Code Review Standards

All changes to main branch MUST undergo peer code review. Reviewers MUST verify:
- Spec compliance (all acceptance criteria met)
- Clean architecture adherence (proper layer separation)
- Security implementation (authorization checks present, IDOR prevented)
- Test coverage (target 80%+ on changed code)
- Documentation completeness (README updated if necessary)
- No architectural debt introduced without documented rationale

### Quality Gates

Pull requests MUST pass all gates before merge:
- All tests green (unit and integration)
- No security vulnerabilities in dependency scan
- Code compiles without warnings
- Architecture rules enforced (layer dependencies correct)
- Spec-driven requirements all addressed

## Governance

### Constitution Authority

This Constitution supersedes all other development practices and guidelines for ContosoDashboard. It establishes non-negotiable principles for Spec-Driven Development, clean architecture, and security. All team members, including future contributors, MUST understand and follow these principles.

### Amendment Procedure

Amendments to this Constitution require:
1. **Formal Proposal**: Document the proposed change with rationale and impact analysis
2. **Discussion**: Collaborate with team leads and architects to discuss implications
3. **Voting**: Consensus or team lead approval required (simple majority at minimum)
4. **Documentation**: Update this Constitution with the change, increment version, and record amendment date
5. **Communication**: Notify all active contributors and document in project wiki/README

### Version Semantics

- **MAJOR** (X.0.0): Backward-incompatible principle removals or redefinitions (rare)
- **MINOR** (X.Y.0): New principle added or materially expanded guidance
- **PATCH** (X.Y.Z): Clarifications, wording refinements, or typo fixes

### Compliance Review

Compliance with this Constitution MUST be verified in pull request reviews and architecture reviews. The following violations warrant PR rejection:
- Implementation without approved spec
- Architectural layer violations
- Missing authorization checks or IDOR vulnerabilities
- Test coverage below 70% on critical paths
- Governance process bypassed

**Version**: 1.0.0 | **Ratified**: 2025-01-15 | **Last Amended**: 2026-08-14
