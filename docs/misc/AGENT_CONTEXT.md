# ClipCore Agent Context

**Mission**: Build **ClipCore**, a multi-tenant SaaS platform for event videographers. Think "PhotoReflect," but for video.

**Context**: 
- **Origin**: This project started as "ClipCore," a single-tenant MVP. 
- **Current State**: We have migrated the codebase to a new repo (`ClipCore`), but the code internals (Namespaces, Solution files, Dockerfiles) still reflect the old "ClipCore" identity.
- **Architectural Goal**: Transform the single-tenant "ClipCore" app into a multi-tenant SaaS where anyone can sign up and create their own video storefront.

**Critical Instruction**: 
When working on this project, always prioritize the **multi-tenant transformation**.
1.  **Refactor**: Finish renaming `ClipCore` -> `ClipCore`.
2.  **Architecture**: Implement Tenant ID separation (Database & Middleware).
3.  **Identity**: Ensure all documentation and UI refers to "ClipCore".

**Prompt for Next Session**:
> "We are building ClipCore (formerly ClipCore), the 'PhotoReflect for Video'. The codebase is currently in a state of transformation from a single-tenant MVP to a multi-tenant SaaS. Please check `docs/TRANSFORMATION_PLAN.md` for the current status."
