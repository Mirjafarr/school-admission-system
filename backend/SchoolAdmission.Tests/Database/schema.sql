
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ==========================================
-- 1. CLASSES
-- ==========================================

CREATE TABLE IF NOT EXISTS classes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(150) NOT NULL UNIQUE,
    total_seats INTEGER NOT NULL CHECK (total_seats > 0),
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ==========================================
-- 2. ADMISSION HOLDS
-- ==========================================

CREATE TABLE IF NOT EXISTS admission_holds (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    class_id UUID NOT NULL REFERENCES classes(id),
    student_id VARCHAR(17) NOT NULL,
    status VARCHAR(20) NOT NULL DEFAULT 'HELD',

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    expires_at TIMESTAMPTZ NOT NULL,
    confirmed_at TIMESTAMPTZ,

    CONSTRAINT admission_holds_status_check
    CHECK (
        status IN (
            'HELD',
            'CONFIRMED',
            'EXPIRED',
            'CANCELLED'
        )
    )
);

CREATE INDEX IF NOT EXISTS
idx_admission_holds_class_status_expiry
ON admission_holds(class_id, status, expires_at);

CREATE UNIQUE INDEX IF NOT EXISTS
idx_unique_active_student_class
ON admission_holds(student_id, class_id)
WHERE status IN ('HELD', 'CONFIRMED');

-- ==========================================
-- 3. STUDENTS
-- ==========================================

CREATE TABLE IF NOT EXISTS students (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),

    student_id VARCHAR(17) NOT NULL UNIQUE,

    password_hash TEXT NOT NULL,

    role VARCHAR(20) NOT NULL DEFAULT 'Student',

    failed_login_attempts INTEGER NOT NULL DEFAULT 0
        CHECK (failed_login_attempts >= 0),

    locked_until TIMESTAMPTZ NULL,

    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ==========================================
-- 4. ENSURE REQUIRED COLUMNS EXIST
-- ==========================================

ALTER TABLE students
ADD COLUMN IF NOT EXISTS failed_login_attempts
INTEGER NOT NULL DEFAULT 0;

ALTER TABLE students
ADD COLUMN IF NOT EXISTS locked_until
TIMESTAMPTZ NULL;

ALTER TABLE students
ADD COLUMN IF NOT EXISTS role
VARCHAR(20) NOT NULL DEFAULT 'Student';

-- ==========================================
-- 5. ROLE CONSTRAINT (ADD ONLY IF MISSING)
-- ==========================================

DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1
        FROM pg_constraint
        WHERE conname = 'students_role_check'
          AND conrelid = 'students'::regclass
    ) THEN
        ALTER TABLE students
        ADD CONSTRAINT students_role_check
        CHECK (role IN ('Admin', 'Student'));
    END IF;
END
$$;
