### New Rules

Rule ID  |         Category         | Severity | Notes
---------|--------------------------|----------|--------------------
DPDREF01 | DarkPatterns.Refactoring |   Error  | Indicates that the Annotations and Analyzers have a version mismatch
DPDREF02 | DarkPatterns.Refactoring |  Warning | A symbol marked with `PlannedRemoval` does not have the corresponding ticket in the manifest
DPDREF03 | DarkPatterns.Refactoring |  Warning | A symbol marked with `PlannedRefactor` does not have the corresponding ticket in the manifest
DPDREF04 | DarkPatterns.Refactoring |  Warning | A symbol marked with `PlannedRemoval` is used in a context that must be `PlannedRefactor`
DPDREF05 | DarkPatterns.Refactoring |  Warning | A symbol marked with `PlannedRemoval` is used in a context that must also be `PlannedRemoval`
