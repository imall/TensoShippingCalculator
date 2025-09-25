# Copilot Instructions

<!-- Use this file to provide workspace-specific custom instructions to Copilot. For more details, visit https://code.visualstudio.com/docs/copilot/copilot-customization#_use-a-githubcopilotinstructionsmd-file -->

## Project Context

This is a .NET Console application for calculating Tenso shipping cost consolidation benefits.

## Key Features

- Calculate individual shipping costs for multiple packages using Tenso API
- Calculate consolidated shipping costs with consolidation service fees
- Compare costs to determine if consolidation is more economical
- Handle package dimensions, weights, and shipping methods

## Coding Guidelines

- Use modern C# features and async/await patterns
- Implement proper error handling for API calls
- Use System.Text.Json for JSON serialization
- Follow clean code principles with clear method names
- Add appropriate comments for complex calculations

## API Integration

- Use Tenso's shipping estimation API: `https://www.tenso.com/api/cht/estimate`
- Handle different shipping methods (ECMS, EMS, DHL, etc.)
- Consider both actual weight and volumetric weight calculations
