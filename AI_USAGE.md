# AI Usage Disclosure

## School Admission Management System

This document discloses the use of AI assistance during the development of the School Admission Management System.

AI tools were used as development assistants. The developer remained responsible for reviewing, adapting, implementing, and verifying the final work.

## 1. AI Tools Used

### ChatGPT

ChatGPT was used to assist with:

- Understanding the project requirements.
- Discussing backend architecture and API design.
- Reviewing database schema and relationships.
- Identifying potential issues in admission reservation logic.
- Debugging Docker and Docker Compose configuration.
- Preparing frontend and backend configuration suggestions.
- Reviewing authentication and authorization concepts.
- Drafting project documentation.
- Developing an automated testing strategy.
- Identifying potential concurrency and data-integrity issues.

## 2. Areas Where AI Assistance Was Used

### Backend Development

AI assistance was used to discuss:

- ASP.NET Core Web API structure.
- JWT authentication.
- Role-based authorization.
- PostgreSQL integration using Npgsql.
- Admission reservation lifecycle.
- Database constraints and indexes.

### Frontend Development

AI assistance was used to discuss:

- React and TypeScript implementation.
- API integration.
- Frontend and backend communication.
- Nginx reverse proxy configuration.
- Docker deployment.

### Database Development

AI assistance was used to review:

- Student and class relationships.
- Admission reservation records.
- Seat availability calculations.
- Reservation status transitions.
- Duplicate reservation prevention.

### DevOps and Deployment

AI assistance was used to:

- Prepare Dockerfiles.
- Configure Docker Compose.
- Configure frontend Nginx routing.
- Debug Docker build and NuGet package restoration issues.
- Configure environment variables.
- Preserve existing PostgreSQL data during deployment.

### Documentation and Testing

AI assistance was used to prepare documentation drafts and identify important automated test scenarios, including simultaneous reservation requests.

## 3. Human Review and Verification

AI-generated suggestions were not treated as automatically correct.

The developer reviewed and adapted the suggested changes before applying them.

Verification activities included:

- Running the ASP.NET Core application.
- Checking PostgreSQL database connectivity.
- Testing student and administrator authentication.
- Testing class creation.
- Testing admission reservation and confirmation.
- Checking admission records.
- Building Docker images.
- Starting the Docker Compose services.
- Checking container health and status.

Automated concurrency tests must be executed and their actual results recorded before final submission.

## 4. Limitations and Responsibility

AI-generated suggestions may contain errors, incomplete assumptions, or implementation details that do not match the final application.

The developer is responsible for:

- Understanding the submitted source code.
- Reviewing all AI-assisted changes.
- Ensuring correctness and security.
- Running the application and automated tests.
- Explaining the implementation during technical evaluation.

## 5. Transparency

AI assistance was used as a supporting development resource, not as a replacement for developer review or responsibility.

The final repository should reflect the actual implementation, verified behavior, and work performed by the developer.