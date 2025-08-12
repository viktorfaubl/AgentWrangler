# Copilot Agent Instructions

This is an Avalonia based multi OS targeting project.
For research use Context7.
Always do a research for used libraries.

Create Clean Code. Follow SOLID always.

Always try to build the code and fix all errors.

Project structure, always follow this : 
Here is a list of all projects in the solution and their intended purpose:
1.	AgentWrangler
•	Purpose: Core shared logic, view models, and UI definitions for the application. Contains Avalonia UI components and resources. Used as the main library by all platform-specific projects.
2.	AgentWrangler.Desktop
•	Purpose: Desktop application entry point (Windows, MacOS, Linux). Hosts the Avalonia UI for desktop environments. References the core AgentWrangler project.
3.	AgentWrangler.Android
•	Purpose: Android application entry point. Hosts the Avalonia UI for Android devices. References the core AgentWrangler project.
4.	AgentWrangler.iOS
•	Purpose: iOS application entry point. Hosts the Avalonia UI for iOS devices. References the core AgentWrangler project.
5.	AgentWrangler.Browser
•	Purpose: WebAssembly/browser application entry point. Hosts the Avalonia UI for web browsers. References the core AgentWrangler project.
Each platform-specific project provides the necessary startup code and configuration to run the shared Avalonia UI on its respective platform.