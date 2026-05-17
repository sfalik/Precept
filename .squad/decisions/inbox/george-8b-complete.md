# George Slice 8b Complete

- Parser change summary: construction success rows are uniformly `on <Event> -> ...` `EventRow` constructs; the inline row-level `initial` token is no longer part of construction-row parsing, and reject routing remains the secondary `-> reject` pass.
- TypeChecker change summary: construction classification comes from the bound event's `IsInitial` metadata; event-handler normalization no longer carries the dead `ConstructionRow` success-path lane, and reject-path detection keys off the emitted reject slot instead of construction-row kind branching.
- Test files swept: 169 C# files under `test/Precept.Tests` (`on \w+ initial` matches: 0).
- Sample files swept: 30 `.precept` files under `samples` (`on \w+ initial` matches: 0).
- Test count: `dotnet test test\Precept.Tests\ --no-build --nologo -v minimal` ran 5,796 tests: 5,794 passed, 2 failed (`F5TempVerify` known pre-existing `UnsatisfiableInitialState` cases for `parcel-locker-pickup.precept` and `clinic-appointment-scheduling.precept`).
