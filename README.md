# AgentWrangler

A cross-platform C# application developed in Visual Studio 2022 that uses OCR to capture your entire screen, transcribes your command, and sends it to a selected language model powered by Groq for inference.
Features

Screen OCR: Captures and extracts text from your entire screen.
Transcription: Transcribes spoken commands.
LLM Inference: Sends transcribed commands to a Groq-powered language model for processing.
Cross-Platform: Designed to work across multiple platforms (Windows, macOS, Linux), though currently tested only on Windows.

Requirements

Visual Studio 2022 (with .NET SDK 6.0 or higher)
Groq API key (sign up at https://console.groq.com/keys)
Required NuGet packages (listed in the project’s .csproj file)

Installation

Clone the Repository
Open the .sln file in Visual Studio 2022.
Restore NuGet packages (Visual Studio will prompt you, or run):dotnet restore

Select a Model:

The application will prompt you to choose a Groq model for inference (e.g., available models will be listed).


Provide a Command:

Speak or type your command. The application will use OCR to capture screen content and transcribe your command.
The transcribed command and screen data are sent to the selected Groq model for processing.

View Results:

The model's response will be displayed in the application interface.


Platform Support

Tested: Windows
Untested: macOS, Linux (cross-platform support is implemented using .NET but not fully validated)

Development Notes

The application uses Groq for fast and efficient LLM inference. Ensure you have a valid API key.
Screen OCR and Transcript also sent to Groq endpoints for processing.
Currently optimized for Windows; testing on other platforms is ongoing.

Contributing
Contributions are welcome! Please follow these steps:

Fork the repository.
Create a new branch (git checkout -b feature/your-feature).
Commit your changes (git commit -m 'Add your feature').
Push to the branch (git push origin feature/your-feature).
Open a pull request.

License
This project is licensed under the MIT License. See the LICENSE file for details.
Contact
For questions or support, please open an issue or contact viktor.faubl@gmail.com.