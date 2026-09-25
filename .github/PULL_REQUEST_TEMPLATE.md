## Overview
<!-- What does this change do, in one or two sentences? Link the checklist item from the build guide. -->

## Type of Change
- [ ] New feature
- [ ] Bug fix
- [ ] Refactor / tech debt
- [ ] Infrastructure (Docker, migrations, pipeline)
- [ ] Documentation

## Technical Implementation
<!-- Which projects changed and why. Call out new gRPC contracts, new events, new EF migrations. -->

## Testing and Verification
<!-- Numbered, reproducible steps. Screenshots or a recording for anything visual. -->
1.
2.
3.

## Risks and Deployment Considerations
- [ ] Adds or changes an EF Core migration
- [ ] Adds or changes a docker-compose service
- [ ] Changes a `.proto` contract (breaking for consumers?)
- [ ] Changes an event contract in `Helpdesk.Contracts`

## Developer Checklist
- [ ] Self-reviewed my own diff as if it were someone else's PR
- [ ] Comments added only where the code is non-obvious
- [ ] No credentials or connection-string passwords committed
- [ ] Build guide / README updated if behaviour changed
