# Citizenhackathon2025.API

markdown
# CitizenHackathon2025.API / OutZen

🏆 Proud Winner of Two Technical Awards at the Citizen of Wallonia Hackathon (Charleroi, 2025)  
An advanced, production-ready enterprise solution leveraging a secure .NET Core Architecture integrated with Artificial Intelligence (Mistral).

---

## 🚀 Overview

OutZen is a modern software solution designed to solve [expliquer le problème en 1 phrase, ex: "streamline local administrative workflows"]. 
Built during the 2025 Citizen of Wallonia hackathon, this project demonstrates how to combine rapid prototyping with enterprise-grade architecture, strict security standards, and practical AI capabilities.

## 🛠️ Tech Stack & Architecture

This project is built using a Clean Architecture / Domain-Driven Design (DDD) pattern to ensure scalability, maintainability, and clean separation of concerns.

*   Backend: .NET 6 / 8 Web API (C#)
*   Database: Entity Framework Core with SQL Server / CosmosDB
*   Frontend: Blazor 
*   Cloud / DevOps: Azure / Docker / GitHub Actions

---

## 🧠 AI Integration

Unlike simple API wrappers, the AI component is deeply integrated into the business logic to deliver autonomous value:

*   Framework: Built using Semantic Kernel to orchestrate LLM orchestration.
*   Features: Implemented automated predictive analytics.
*   Efficiency: Designed with prompt caching and token optimization to control cloud operational costs.

---

## 🔒 Cybersecurity & Hardening

Security was treated as a first-class citizen from day one, adhering to modern DevSecOps and OWASP guidelines:

*   Authentication & AuthZ: Secure token handling using JWT and Argon2id.
*   Data Protection: Implementation of AES-256 encryption for sensitive database fields and strict HTTPS/TLS enforcement.
*   Secret Management: Zero hardcoded credentials; utilizing secure environment variables [or Azure Key Vault].
*   Defensive Coding: Protection against SQL Injection, XSS, and CSRF built directly into the .NET pipeline.

---

## ⚡ Key Highlights Datas award for the database and the API architecture and the WallonieEnPoche award for the idea and the possibility of supplementing another application.

1.  Production-Ready Blueprint: Despite the hackathon time constraints, the repository features clean code, unit tests, and structured logging.
2.  Innovative AI UX: The AI integration focuses on user accessibility and reliable, non-hallucinatory outputs.
3.  Secure by Design: Proven resistance to common vulnerability vectors during technical evaluation.

---

## 📦 Getting Started

### Prerequisites
*   .NET SDK 
*   Local Database engine

### Installation
1. Clone the repository:
   ```bash
   git clone https://github.com
   ```
2. Configure your environment variables in `appsettings.json` (do not commit secrets!).
3. Run the migrations and start the project:
   ```bash
   dotnet run
   ```
