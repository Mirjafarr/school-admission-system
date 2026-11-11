# Assumptions and Design Decisions

## School Admission Management System

This document describes the business rules, assumptions, and design decisions used when implementing the School Admission Management System.

Where the assignment does not explicitly specify implementation details, the following assumptions guide the system.

## 1. User Roles

The system supports two user roles:

- **Admin:** Manages classes, seat capacity, student records, and admission records.
- **Student:** Registers an account, logs in, views classes, and manages personal admission reservations.

Newly registered accounts receive the Student role by default.

Administrator accounts are provisioned through an authorized administrative process.

## 2. Student Identification

- Each student has a unique student ID.
- Student IDs are used for authentication and identification.
- A student account cannot be registered with an already existing student ID.
- Passwords are stored as password hashes, not plaintext.

## 3. Class and Seat Management

- Each class and shift is represented by a separate class record.
- Every class has a positive integer seat capacity.
- Class names must be unique.
- Only available seats can be reserved.
- Total capacity must not be exceeded by active reservations and confirmed admissions.

## 4. Admission Reservation Lifecycle

A student does not receive a confirmed admission immediately after requesting a reservation.

The reservation process has four possible states:

| Status | Meaning |
|---|---|
| HELD | A seat is temporarily reserved. |
| CONFIRMED | The student has successfully confirmed admission. |
| EXPIRED | The reservation period has ended without confirmation. |
| CANCELLED | The reservation was cancelled. |

### Reservation assumptions

1. A reservation temporarily occupies one seat.
2. The current implementation uses a 10-minute reservation period, as reflected in the application's admission records.
3. A student must confirm the reservation before its expiry.
4. An expired reservation cannot be confirmed.
5. A cancelled reservation releases its seat.
6. A confirmed admission permanently consumes one seat unless an additional cancellation policy is implemented.

## 5. Seat Availability

Seat availability is determined using:

**Available Seats = Total Seats - Confirmed Seats - Active Held Seats**

An active held reservation is one whose status is HELD and whose expiry time has not passed.

Expired and cancelled reservations do not consume available capacity.

The database and backend must enforce this rule consistently, including when multiple students request seats simultaneously.

## 6. Duplicate Reservation Prevention

A student should not have multiple active reservations for the same class.

The database uses a partial unique index covering student ID and class ID for HELD and CONFIRMED reservations.

This helps prevent duplicate active reservations.

## 7. Concurrency Handling

Simultaneous reservation requests must not result in overbooking.

The expected behavior is:

- All requests must be checked against the latest available capacity.
- Seat allocation must be performed atomically.
- Concurrent requests must not reserve more seats than the class capacity.
- Database constraints and transaction handling must protect data consistency.

Concurrency behavior must be verified through automated tests against PostgreSQL.

## 8. Reservation Expiration

- Reservation expiration is determined using the database timestamp.
- Timestamps are stored using PostgreSQL TIMESTAMPTZ.
- A reservation is no longer active after its expiry time.
- Expired reservations must not be counted as active seat holds.

## 9. Authentication and Authorization

- JWT Bearer Authentication is used for protected API endpoints.
- Administrative endpoints require the Admin role.
- Student endpoints require an authenticated Student account.
- Passwords are stored using password hashing.
- Failed login attempts and account lockout information are maintained.

## 10. Database Decisions

PostgreSQL is used as the primary relational database.

The main entities are:

- Students
- Classes
- Admission Holds

UUIDs are used for primary identifiers where appropriate.

Foreign keys maintain relationships between classes and admission reservations.

Database constraints and indexes are used to support data integrity and reservation uniqueness.

## 11. Out-of-Scope Features

Unless explicitly required by the assessment, the following features are not assumed:

- Online admission fee or payment gateway integration.
- SMS and email notification services.
- External educational board integration.
- Automated document verification.
- Multiple school branch management.

## 12. Summary

The main design priority is to maintain accurate seat availability and prevent overbooking while providing a clear admission process for students and administrators.

These assumptions are intended to make the implementation predictable, maintainable, and consistent with the assignment requirements.