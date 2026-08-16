# This jq filter transforms a mutation testing report to the SonarQube generic issue import format.

.framework.name as $frameworkName
| .projectRoot as $projectRoot
| ($frameworkName // "Mutation Testing") as $engineId
| .files
| to_entries
| {
    rules: [
        {
            id: "MutantSurvived",
            name: "Surviving mutant",
            description: "A mutant survived after running the tests, which means the tests do not detect the introduced change.",
            engineId: $engineId,
            cleanCodeAttribute: "TESTED",
            impacts: [
                {
                    softwareQuality: "MAINTAINABILITY",
                    severity: "MEDIUM"
                }
            ]
        },
        {
            id: "MutantNoCoverage",
            name: "Uncovered mutant",
            description: "A mutant was not covered by any of the tests.",
            engineId: $engineId,
            cleanCodeAttribute: "TESTED",
            impacts: [
                {
                    softwareQuality: "MAINTAINABILITY",
                    severity: "LOW"
                }
            ]
        }
    ],
    issues: map(
        .value.mutants[] as $mutants
        | del(.value) as $file
        | $mutants
        | select(.status == ("Survived", "NoCoverage"))
        | (
            if .replacement then
                "The " + .mutatorName + " was mutated to " + .replacement + " without any tests failing."
            else
                "The " + .mutatorName + " was mutated without any tests failing."
            end
        ) as $mutation
        | {
            ruleId: ("Mutant" + .status),
            effortMinutes: 10,
            primaryLocation: {
                message: (
                    if .status == "NoCoverage" then
                        "A mutant was not covered by any of the tests. " + $mutation
                    else
                        "A mutant survived after running the tests. " + $mutation
                    end
                ),
                filePath: (
                    if $projectRoot then
                        $file.key | sub("^" + $projectRoot + "/"; "")
                    else
                        $file.key
                    end
                ),
                textRange: {
                    startLine: .location.start.line,
                    endLine: .location.end.line,
                    startColumn: (.location.start.column - 1),
                    endColumn: (.location.end.column - 1)
                }
            }
        }
    )
}
