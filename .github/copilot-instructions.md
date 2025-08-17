# GitHub Copilot Custom Instructions for This Repository

- All code must target .NET 8 and use C# 12 features and syntax[4][9].
- Follow the official C# 12 coding conventions and best practices.
- Use modern C# 12 features such as:
- All generated code must be idiomatic, clear, and maintainable.
- Use XML documentation comments for all public types and members.
- Prefer expressive variable and method names; avoid abbreviations.
- Write unit tests for all new methods using xUnit.
- Avoid obsolete patterns and APIs; use the latest .NET 9 libraries.
- Ensure thread safety and proper use of async/await patterns.
- Use dependency injection for services and data access.
- All code should compile without warnings or errors.
- Include comments where logic is non-trivial.

# Project Structure Guidelines

- Organize code into folders by feature or domain.
- Place interfaces in an `Interfaces` folder.
- Place implementation classes in appropriate feature folders.
- Place unit tests in a parallel `Tests` project.
- Xunit with xunit assert

# Example Usage

- When generating a service, use constructor injection for dependencies.
- When writing async methods, always use `Task` or `Task<T>` return types and include `Async` in method names.