# Projects and dependencies analysis

This document provides a comprehensive overview of the projects and their dependencies in the context of upgrading to .NETCoreApp,Version=v10.0.

## Table of Contents

- [Executive Summary](#executive-Summary)
  - [Highlevel Metrics](#highlevel-metrics)
  - [Projects Compatibility](#projects-compatibility)
  - [Package Compatibility](#package-compatibility)
  - [API Compatibility](#api-compatibility)
- [Aggregate NuGet packages details](#aggregate-nuget-packages-details)
- [Top API Migration Challenges](#top-api-migration-challenges)
  - [Technologies and Features](#technologies-and-features)
  - [Most Frequent API Issues](#most-frequent-api-issues)
- [Projects Relationship Graph](#projects-relationship-graph)
- [Project Details](#project-details)

  - [src\StateMachine\StateMachine.csproj](#srcstatemachinestatemachinecsproj)
  - [test\StateMachine.Tests\StateMachine.Tests.csproj](#teststatemachinetestsstatemachinetestscsproj)


## Executive Summary

### Highlevel Metrics

| Metric | Count | Status |
| :--- | :---: | :--- |
| Total Projects | 2 | All require upgrade |
| Total NuGet Packages | 5 | All compatible |
| Total Code Files | 6 |  |
| Total Code Files with Incidents | 3 |  |
| Total Lines of Code | 1593 |  |
| Total Number of Issues | 4 |  |
| Estimated LOC to modify | 2+ | at least 0.1% of codebase |

### Projects Compatibility

| Project | Target Framework | Difficulty | Package Issues | API Issues | Est. LOC Impact | Description |
| :--- | :---: | :---: | :---: | :---: | :---: | :--- |
| [src\StateMachine\StateMachine.csproj](#srcstatemachinestatemachinecsproj) | net6.0 | 🟢 Low | 0 | 0 |  | ClassLibrary, Sdk Style = True |
| [test\StateMachine.Tests\StateMachine.Tests.csproj](#teststatemachinetestsstatemachinetestscsproj) | net6.0 | 🟢 Low | 0 | 2 | 2+ | DotNetCoreApp, Sdk Style = True |

### Package Compatibility

| Status | Count | Percentage |
| :--- | :---: | :---: |
| ✅ Compatible | 5 | 100.0% |
| ⚠️ Incompatible | 0 | 0.0% |
| 🔄 Upgrade Recommended | 0 | 0.0% |
| ***Total NuGet Packages*** | ***5*** | ***100%*** |

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 2 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 952 |  |
| ***Total APIs Analyzed*** | ***954*** |  |

## Aggregate NuGet packages details

| Package | Current Version | Suggested Version | Projects | Description |
| :--- | :---: | :---: | :--- | :--- |
| coverlet.collector | 3.1.2 |  | [StateMachine.Tests.csproj](#teststatemachinetestsstatemachinetestscsproj) | ✅Compatible |
| FluentAssertions | 6.5.1 |  | [StateMachine.Tests.csproj](#teststatemachinetestsstatemachinetestscsproj) | ✅Compatible |
| Microsoft.NET.Test.Sdk | 17.1.0 |  | [StateMachine.Tests.csproj](#teststatemachinetestsstatemachinetestscsproj) | ✅Compatible |
| xunit | 2.4.1 |  | [StateMachine.Tests.csproj](#teststatemachinetestsstatemachinetestscsproj) | ✅Compatible |
| xunit.runner.visualstudio | 2.4.3 |  | [StateMachine.Tests.csproj](#teststatemachinetestsstatemachinetestscsproj) | ✅Compatible |

## Top API Migration Challenges

### Technologies and Features

| Technology | Issues | Percentage | Migration Path |
| :--- | :---: | :---: | :--- |

### Most Frequent API Issues

| API | Count | Percentage | Category |
| :--- | :---: | :---: | :--- |
| M:System.Exception.#ctor(System.Runtime.Serialization.SerializationInfo,System.Runtime.Serialization.StreamingContext) | 2 | 100.0% | Source Incompatible |

## Projects Relationship Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart LR
    P1["<b>📦&nbsp;StateMachine.csproj</b><br/><small>net6.0</small>"]
    P2["<b>📦&nbsp;StateMachine.Tests.csproj</b><br/><small>net6.0</small>"]
    P2 --> P1
    click P1 "#srcstatemachinestatemachinecsproj"
    click P2 "#teststatemachinetestsstatemachinetestscsproj"

```

## Project Details

<a id="srcstatemachinestatemachinecsproj"></a>
### src\StateMachine\StateMachine.csproj

#### Project Info

- **Current Target Framework:** net6.0
- **Proposed Target Framework:** net10.0
- **SDK-style**: True
- **Project Kind:** ClassLibrary
- **Dependencies**: 0
- **Dependants**: 1
- **Number of Files**: 4
- **Number of Files with Incidents**: 1
- **Lines of Code**: 775
- **Estimated LOC to modify**: 0+ (at least 0.0% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph upstream["Dependants (1)"]
        P2["<b>📦&nbsp;StateMachine.Tests.csproj</b><br/><small>net6.0</small>"]
        click P2 "#teststatemachinetestsstatemachinetestscsproj"
    end
    subgraph current["StateMachine.csproj"]
        MAIN["<b>📦&nbsp;StateMachine.csproj</b><br/><small>net6.0</small>"]
        click MAIN "#srcstatemachinestatemachinecsproj"
    end
    P2 --> MAIN

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 0 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 333 |  |
| ***Total APIs Analyzed*** | ***333*** |  |

<a id="teststatemachinetestsstatemachinetestscsproj"></a>
### test\StateMachine.Tests\StateMachine.Tests.csproj

#### Project Info

- **Current Target Framework:** net6.0
- **Proposed Target Framework:** net10.0
- **SDK-style**: True
- **Project Kind:** DotNetCoreApp
- **Dependencies**: 1
- **Dependants**: 0
- **Number of Files**: 5
- **Number of Files with Incidents**: 2
- **Lines of Code**: 818
- **Estimated LOC to modify**: 2+ (at least 0.2% of the project)

#### Dependency Graph

Legend:
📦 SDK-style project
⚙️ Classic project

```mermaid
flowchart TB
    subgraph current["StateMachine.Tests.csproj"]
        MAIN["<b>📦&nbsp;StateMachine.Tests.csproj</b><br/><small>net6.0</small>"]
        click MAIN "#teststatemachinetestsstatemachinetestscsproj"
    end
    subgraph downstream["Dependencies (1"]
        P1["<b>📦&nbsp;StateMachine.csproj</b><br/><small>net6.0</small>"]
        click P1 "#srcstatemachinestatemachinecsproj"
    end
    MAIN --> P1

```

### API Compatibility

| Category | Count | Impact |
| :--- | :---: | :--- |
| 🔴 Binary Incompatible | 0 | High - Require code changes |
| 🟡 Source Incompatible | 2 | Medium - Needs re-compilation and potential conflicting API error fixing |
| 🔵 Behavioral change | 0 | Low - Behavioral changes that may require testing at runtime |
| ✅ Compatible | 619 |  |
| ***Total APIs Analyzed*** | ***621*** |  |

