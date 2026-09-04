# Threshold Units

[← Back to README](../README.md)

- [Threshold Units](#threshold-units)
  - [Percentage Thresholds](#percentage-thresholds)



## Threshold Units

Use these units with `--threshold-mean` (`-tm`) and `--threshold-allocation` (`-ta`).

**Time (`-tm`):**

| Unit | Description | Example |
|------|-------------|---------|
| `ns` | Nanoseconds | `100ns` |
| `us` | Microseconds | `10us` |
| `ms` | Milliseconds | `10ms` |
| `s` | Seconds | `1s` |

**Memory (`-ta`):**

| Unit | Description | Example |
|------|-------------|---------|
| `b` | Bytes | `10b` |
| `kb` | Kilobytes | `10kb` |
| `mb` | Megabytes | `10mb` |
| `gb` | Gigabytes | `1gb` |

### Percentage Thresholds

`compare` also accepts `%` for time and memory thresholds. A percentage is relative to the baseline value, for example `5%`.

`gate` accepts only absolute time and memory thresholds because it checks one report without a baseline. It does not accept `%`.