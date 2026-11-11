\# School Admission Management System



A full-stack, Dockerized School Admission Management System built for the \*\*Onnorokom Projukti Limited – Assistant Software Engineer Take-Home Assignment\*\*.



The system manages school classes, limited seat availability, student registrations, temporary admission reservations, and admission confirmations using a first-come, first-served admission process.



\## 1. Technology Stack



| Layer | Technology |



|---|---|



| Backend | C# / ASP.NET Core 10 |



| Database | PostgreSQL 16 |



| Database access | Npgsql |



| Frontend | React 19, TypeScript |



| Build tool | Vite |



| UI styling | Tailwind CSS |



| Icons | Lucide React |



| Charts | Recharts |



| Authentication | JWT Bearer Authentication |



| Web server | Nginx |



| Containerization | Docker and Docker Compose |



| Backend testing | xUnit and Testcontainers for PostgreSQL |



The implementation uses versions newer than the assignment's minimum requirements of .NET 8+ and React 18+.



\## 2. Project Overview



The school admission process follows a first-come, first-served policy. Each class and shift has a limited number of seats.



When a student applies for admission:



1\. The system checks the selected class's seat availability.



2\. An available seat can be temporarily held for the student.



3\. The student must confirm the admission within the reservation period.



4\. A confirmed reservation becomes a confirmed admission.



5\. An expired or cancelled reservation releases the seat for other students.



The system provides separate administrative and student functionality.



\## 3. Key Features



\### Student functionality



\- Student account registration and login.



\- JWT-based authentication.



\- Browse available classes and seat information.



\- Request a temporary admission reservation.



\- View reservation details.



\- Confirm an active reservation.



\- Cancel a reservation.



\- View personal admission history.



\### Administrator functionality



\- Secure administrator authentication.



\- Dashboard with admission and seat statistics.



\- Create and manage classes.



\- Configure class seat capacity.



\- View registered students.



\- Search student records.



\- View admission and reservation records.



\- Monitor confirmed, held, expired, and cancelled admissions.



\### Admission management



The system uses the following reservation states:



| Status | Description |



|---|---|



| HELD | A seat is temporarily reserved. |



| CONFIRMED | Admission has been confirmed. |



| EXPIRED | The reservation period has expired. |



| CANCELLED | The reservation has been cancelled. |



Seat availability is calculated using total capacity and active reservations.



\## 4. System Architecture



```text



                 React Frontend



                   TypeScript



                       |



                       |



                 Nginx Reverse Proxy



                       |



                       |



                ASP.NET Core API



                       |



                       |



                  Npgsql



                       |



                       |



                 PostgreSQL 16



```



The application consists of three Docker services:



\- \*\*Frontend:\*\* React application served by Nginx.



\- \*\*Backend:\*\* ASP.NET Core Web API.



\- \*\*Database:\*\* PostgreSQL with persistent storage.



The frontend communicates with the backend through the `/api` reverse-proxy path in the Docker deployment.



\## 5. Repository Structure



```text



school-admission-system/



│



├── backend/



│   ├── SchoolAdmission.Api/



│   │   ├── Controllers/



│   │   ├── Properties/



│   │   ├── Program.cs



│   │   ├── appsettings.json



│   │   ├── Dockerfile



│   │   └── SchoolAdmission.Api.csproj



│   │



│   └── SchoolAdmission.Tests/



│       ├── Database/



│       │   └── schema.sql



│       └── SchoolAdmission.Tests.csproj



│



├── frontend/



│   ├── public/



│   ├── src/



│   ├── Dockerfile



│   ├── nginx.conf



│   ├── package.json



│   └── vite.config.ts



│



├── .dockerignore



├── .env.example



├── .gitignore



├── docker-compose.yml



├── ASSUMPTIONS.md



├── AI\_USAGE.md



└── README.md



```



\## 6. Prerequisites



For Docker deployment:



\- Docker Desktop



\- Docker Compose



\- Git



For local development:



\- .NET 10 SDK



\- Node.js 22+



\- npm



\- PostgreSQL 16 (if not using the Docker database)



\## 7. Running the Application with Docker



\### Step 1: Clone the repository



```bash



git clone https://github.com/YOUR\_USERNAME/school-admission-system.git



cd school-admission-system



```



Replace `YOUR\_USERNAME` with the GitHub repository owner's username.



\### Step 2: Configure environment variables



Copy the example environment file:



\*\*PowerShell:\*\*



```powershell



Copy-Item .env.example .env



```



Update `.env` with your own database password and a unique JWT secret.



The JWT secret must contain at least 32 bytes.



Never commit `.env` to GitHub.



\### Step 3: Build and start all services



```bash



docker compose up --build



```



Docker Compose starts:



\- PostgreSQL database



\- ASP.NET Core backend



\- React frontend with Nginx



No separate frontend, backend, or database startup commands are required.



\### Step 4: Access the application



| Service | URL |



|---|---|



| Frontend | http://localhost:8080 |



| Backend Swagger | http://localhost:5069/swagger |



| Database health | http://localhost:5069/db-health |



| PostgreSQL | localhost:5432 |



\### Step 5: Check running containers



```bash



docker compose ps



```



\### Step 6: View application logs



```bash



docker compose logs -f



```



View individual service logs:



```bash



docker compose logs -f backend



docker compose logs -f frontend



docker compose logs -f postgres



```



\### Step 7: Stop the application



```bash



docker compose down



```



This stops the containers while retaining the PostgreSQL data volume.



Do not use `docker compose down -v` unless you intentionally want to delete the database volume.



\## 8. API Endpoints



The API is documented using Swagger.



\### Authentication



| Method | Endpoint | Purpose |



|---|---|---|



| POST | `/auth/register` | Register an account |



| POST | `/auth/login` | Authenticate an account |



\### Classes and reservations



| Method | Endpoint | Purpose |



|---|---|---|



| POST | `/classes` | Create a class |



| GET | `/classes` | List classes |



| GET | `/classes/{classId}` | Get class details |



| POST | `/classes/{classId}/holds` | Create a seat reservation |



| POST | `/classes/{classId}/holds/{holdId}/confirm` | Confirm a reservation |



| GET | `/holds/{holdId}` | Get reservation details |



| DELETE | `/holds/{holdId}` | Cancel a reservation |



| GET | `/me/holds` | Get the authenticated student's reservations |



\### Administration



| Method | Endpoint | Purpose |



|---|---|---|



| GET | `/admin/dashboard` | Retrieve dashboard statistics |



| GET | `/admin/students` | List registered students |



| GET | `/admin/admissions` | List admission records |



\### Health check



| Method | Endpoint | Purpose |



|---|---|---|



| GET | `/db-health` | Verify database connectivity |



Protected endpoints require an appropriate JWT Bearer token. Administrative endpoints require the Admin role.



\## 9. Database Design



The main database tables are:



\### students



Stores student and administrator account information.



Important fields:



\- id



\- student\_id



\- password\_hash



\- role



\- failed\_login\_attempts



\- locked\_until



\- created\_at



\### classes



Stores available classes and their seat capacities.



Important fields:



\- id



\- name



\- total\_seats



\- created\_at



\### admission\_holds



Stores admission reservations and their lifecycle.



Important fields:



\- id



\- class\_id



\- student\_id



\- status



\- created\_at



\- expires\_at



\- confirmed\_at



The database uses UUID primary keys and foreign-key relationships.



The database schema is provided in:



`backend/SchoolAdmission.Tests/Database/schema.sql`



\## 10. Authentication and Security



\- JWT Bearer Authentication.



\- Role-based authorization for Admin and Student accounts.



\- Password hashing.



\- Failed-login attempt tracking and account lockout.



\- Environment-based configuration for database credentials and JWT settings.



\- Protected administrative endpoints.



No production credentials or JWT secrets should be committed to the repository.



\## 11. Running Backend Tests



The backend test project uses xUnit and PostgreSQL Testcontainers.



Run:



```bash



dotnet test backend/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj



```



Docker must be running for tests that use PostgreSQL Testcontainers.



The automated test suite is being completed to verify authentication, admission reservation lifecycle, and concurrent seat allocation.



The concurrency test must verify that simultaneous reservation requests cannot exceed the available seat capacity.



Only report tests as passing after verifying their actual execution results.



\## 12. Assumptions and Design Decisions



The assignment leaves some implementation details open to reasonable interpretation.



The project's decisions and their justifications are documented separately in:



`ASSUMPTIONS.md`



\## 13. AI Tool Usage



AI-assisted development and verification are documented in:



`AI\_USAGE.md`



The documentation identifies where AI assistance was used, how suggestions were reviewed, and how implementation decisions were verified.



\## 14. Configuration Reference



The following environment variables are used by the Docker deployment:



| Variable | Purpose |



|---|---|



| POSTGRES\_PASSWORD | PostgreSQL database password |



| JWT\_SECRET | JWT signing secret |



| JWT\_ISSUER | JWT issuer |



| JWT\_AUDIENCE | JWT audience |



The frontend API URL is configurable using:



`VITE\_API\_URL`



For Docker deployment, it is configured as `/api`.



\### Automated Test Results



The backend automated test suite was executed successfully.



| Metric | Result |

|---|---:|

| Total tests | 25 |

| Passed | 25 |

| Failed | 0 |

| Skipped | 0 |

| Duration | 12.3 seconds |



Run the tests using:



```bash

dotnet test backend/SchoolAdmission.Tests/SchoolAdmission.Tests.csproj



\## 15. Assignment Submission



This project was developed for the Assistant Software Engineer recruitment take-home assignment of Onnorokom Projukti Limited.



The repository is intended to be publicly accessible and contains the application source code, Docker configuration, documentation, and automated tests.



\---



\*\*School Admission Management System\*\*



Built with ASP.NET Core, React, PostgreSQL, and Docker.

