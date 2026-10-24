# CitizenHackathon2025.API / OutZen

🏆 **Proud Winner of Two Technical Awards at the Citizen of Wallonia Hackathon (Charleroi, 2025)**  
*   **Data Award:** For Database Design and API Architecture.
*   **WallonieEnPoche Award:** For Project Concept and Ecosystem Integration Capabilities.

An advanced, production-ready enterprise solution leveraging a secure .NET Core Architecture integrated with Artificial Intelligence (Mistral AI).

---

## 🚀 Overview

**OutZen** is a smart tourism assistant designed to optimize user experience during outdoor activities and site visits. It dynamically tracks environmental factors and delivers **real-time (SignalR) alternative suggestions** when disruptions occur. 

The application contextualizes recommendations by cross-referencing multiple live data streams:
*   **Weather Conditions:** OneWeather Forecast
*   **Traffic & Mobility:** Perex / Waze / ODWB (Open Data Wallonie-Bruxelles)
*   **Emergency Alerts:** BeAlert integration for natural disasters or safety hazards
*   **Crowd Density:** Estimated via cell tower infrastructure metrics from the *Géoportail de la Wallonie* registry.

---

## 🛠️ Tech Stack & Architecture

This project is built using **Clean Architecture** and **Domain-Driven Design (DDD)** patterns to guarantee strict separation of concerns, high scalability, and clean testing boundaries.

*   **Backend:** Ecosystem hybrid combining .NET 8 ASP.NET Core Web API (C#) & FastAPI (Python)
*   **Database:** Entity Framework Core with SQL Server & CosmosDB / MongoDB
*   **Frontend:** Blazor 
*   **Cloud & DevOps:** Azure / Docker / GitHub Actions CI-CD pipelines

---

## 🧠 AI Integration

Instead of acting as a simple API wrapper, the AI ecosystem is deeply woven into the platform's core workflows:

*   **Orchestration Framework:** Leverages **Semantic Kernel** to manage LLM agents and semantic routing.
*   **Core Model:** Powered by **Mistral AI** for intelligent decision-making and predictive analytics.
*   **Cost Efficiency:** Engineered with customized prompt caching and token optimization to manage cloud operational expenses.

---

## 🔒 Cybersecurity & Hardening

Security and defensive engineering were treated as core architecture principles from day one, aligning with DevSecOps practices and OWASP standards:

*   **Authentication & AuthZ:** Secure token lifecycle management utilizing stateless **JWT** paired with **Argon2id** for cryptographic password hashing.
*   **Data Protection:** At-rest AES-256 encryption for sensitive database fields and strict in-transit HTTPS/TLS configuration.
*   **Secret Management:** Strict zero-hardcoded credential policy, leveraging secure OS Environment Variables and secure Secrets handlers.
*   **Defensive Coding:** Custom .NET middleware pipeline hardened against SQL Injection, Cross-Site Scripting (XSS), and CSRF.

---

## ⚡ Key Architectural Highlights

1.  **Production-Ready Blueprint:** Engineered under hackathon constraints without sacrificing clean code, robust unit test coverage, or structured semantic logging.
2.  **Modular Interoperability:** Designed from inception to seamlessly supplement or plug into existing regional smart-city applications (such as *Wallonie en Poche*).
3.  **Secure by Design:** Audited and proven resilient against standard vulnerability vectors during the live hackathon technical evaluation.

---

## 📦 Getting Started

### Prerequisites
*   .NET SDK (v8.0+)
*   Python 3.10+ (for FastAPI services)
*   Local SQL Server or MongoDB instance

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com/POLLESSI/Citizenhackathon2025.API.git
   ```
2. Configure your environment variables and connection strings in `appsettings.json` (do not commit production secrets).
3. Run migrations and spin up the backend:
   ```bash
   dotnet run
   ```
