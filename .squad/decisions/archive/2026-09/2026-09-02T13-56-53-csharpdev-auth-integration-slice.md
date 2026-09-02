## Decision

For the first E2E-to-integration authentication migration slice, port invalid login by driving the real login message path:

- dispatch `LoginEmailChanged`
- dispatch `LoginPasswordChanged`
- dispatch and drain `LoginSubmitted`
- mock `LoginUser` to return `ApiError`

Do not inject `ApiError` directly in the migrated test. The value of this slice is proving that the reducer issues the login command with the entered credentials and that command failure flows back into the login page state and rendered error UI.

## Scope

Keep the slice local to `Picea.Abies.Conduit.Tests` and avoid expanding into additional auth scenarios until this pattern is established.

## Update 2026-05-05

For the next adjacent auth slice, valid login should use the same harness-first pattern:

- dispatch `LoginEmailChanged`
- dispatch `LoginPasswordChanged`
- dispatch and drain `LoginSubmitted`
- mock `LoginUser` to return `UserAuthenticated(session)`
- capture downstream `PersistSession`, authenticated-home feed fetch, and `NavigationCommand.Push` through `MockCommand<T>` side effects

Do not set authenticated model state directly in the migrated test. The useful assertion is that the success path flows through the same command batch the runtime uses after real authentication.