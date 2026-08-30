// -----------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

// Many CLI command/output tests render through the process-wide
// AnsiConsole.Console (some also swap it for a TestConsole). Running them in
// parallel lets one test's console redirection corrupt another's output on a
// different thread. These tests are fast, so disable assembly parallelization
// to make them deterministic.
[assembly: Xunit.CollectionBehavior(DisableTestParallelization = true)]
