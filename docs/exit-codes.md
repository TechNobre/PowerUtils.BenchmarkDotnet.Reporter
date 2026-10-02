# Exit Codes

[← Back to README](../README.md)

- [Exit Codes](#exit-codes)



## Exit Codes

| Code | Meaning |
|------|---------|
| `0` | Success - no issues detected |
| `1` | Generic error, such as invalid configuration, missing files, or invalid threshold values |
| `2` | Warnings detected when `--fail-on-warnings` is enabled |
| `3` | Performance thresholds exceeded |

For `compare`, code `3` is returned only when `--fail-on-threshold-hit` is enabled. For `gate`, threshold hits always return code `3`.

When both warnings and threshold hits qualify for failure, `compare` returns code `2`, while `gate` returns code `3`.