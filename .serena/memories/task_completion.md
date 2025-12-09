# Cinemarco - Task Completion Checklist

## Before Marking Task Complete

### Build Verification
```bash
# Must succeed without errors
dotnet build

# Run tests  
dotnet test
```

### Backend Checklist
- [ ] Types defined in `src/Shared/Domain.fs`
- [ ] API contract in `src/Shared/Api.fs`
- [ ] Validation in `src/Server/Validation.fs` (if applicable)
- [ ] Domain logic is pure (no I/O) in `src/Server/Domain.fs`
- [ ] Persistence in `src/Server/Persistence.fs`
- [ ] API implementation in `src/Server/Api.fs`

### Frontend Checklist
- [ ] State in appropriate `State.fs` file
- [ ] View in appropriate `View.fs` file
- [ ] Uses GlassPanel for content sections
- [ ] Uses GlassButton for icon actions
- [ ] Uses SectionHeader for section titles
- [ ] Uses RemoteDataView for async data
- [ ] Uses DaisyUI classes for buttons, badges, alerts
- [ ] Poster cards include poster-shine effect

### Quality Checks
- [ ] Tests written (at minimum: domain + validation)
- [ ] `dotnet build` succeeds
- [ ] `dotnet test` passes

## Git Commit Guidelines
- Do NOT include "Co-Authored-By" or any Claude attribution
- All commits attributed solely to repository owner
- Use clear, concise commit messages
