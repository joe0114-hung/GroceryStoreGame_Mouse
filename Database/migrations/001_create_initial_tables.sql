-- GroceryStoreGame initial database schema
-- Database: Supabase PostgreSQL
-- Purpose: Create the first version of all project tables

CREATE TABLE public.users (
    user_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    role VARCHAR(30) NOT NULL,
    birth_year INTEGER,
    gender VARCHAR(20),
    name VARCHAR(100) NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_user_role
        CHECK (role IN ('elder', 'medical_staff')),

    CONSTRAINT chk_birth_year
        CHECK (
            birth_year IS NULL
            OR birth_year BETWEEN 1900 AND 2100
        )
);

CREATE TABLE public.face_profiles (
    face_profile_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id BIGINT NOT NULL UNIQUE,
    face_embedding TEXT NOT NULL,
    created_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),

    CONSTRAINT fk_face_profiles_user
        FOREIGN KEY (user_id)
        REFERENCES public.users(user_id)
        ON DELETE CASCADE
);

CREATE TABLE public.games (
    game_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    group_no INTEGER NOT NULL,
    level_no INTEGER NOT NULL,
    dimension_no INTEGER NOT NULL,
    game_name VARCHAR(100) NOT NULL,

    CONSTRAINT uq_game
        UNIQUE (group_no, level_no, dimension_no),

    CONSTRAINT chk_game_numbers
        CHECK (
            group_no BETWEEN 1 AND 3
            AND level_no BETWEEN 1 AND 3
            AND dimension_no BETWEEN 1 AND 6
        )
);

CREATE TABLE public.training_sessions (
    training_session_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    user_id BIGINT NOT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    ended_at TIMESTAMPTZ,
    completed BOOLEAN NOT NULL DEFAULT FALSE,

    CONSTRAINT fk_training_user
        FOREIGN KEY (user_id)
        REFERENCES public.users(user_id)
        ON DELETE CASCADE,

    CONSTRAINT chk_training_time
        CHECK (
            ended_at IS NULL
            OR ended_at >= started_at
        )
);

CREATE TABLE public.game_sessions (
    game_session_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    training_session_id BIGINT NOT NULL,
    game_id BIGINT NOT NULL,
    started_at TIMESTAMPTZ NOT NULL,
    ended_at TIMESTAMPTZ,
    tremor_count INTEGER NOT NULL DEFAULT 0,

    CONSTRAINT fk_game_training
        FOREIGN KEY (training_session_id)
        REFERENCES public.training_sessions(training_session_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_game
        FOREIGN KEY (game_id)
        REFERENCES public.games(game_id),

    CONSTRAINT uq_training_game
        UNIQUE (training_session_id, game_id),

    CONSTRAINT chk_tremor_count
        CHECK (tremor_count >= 0),

    CONSTRAINT chk_game_session_time
        CHECK (
            ended_at IS NULL
            OR ended_at >= started_at
        )
);

CREATE TABLE public.questions (
    question_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    game_id BIGINT NOT NULL,
    question_code VARCHAR(100) NOT NULL,

    CONSTRAINT fk_question_game
        FOREIGN KEY (game_id)
        REFERENCES public.games(game_id)
        ON DELETE CASCADE,

    CONSTRAINT uq_question_code
        UNIQUE (game_id, question_code)
);

CREATE TABLE public.question_attempts (
    question_attempt_id BIGINT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    game_session_id BIGINT NOT NULL,
    question_id BIGINT NOT NULL,
    attempt_no INTEGER NOT NULL,
    prompt_finished_at TIMESTAMPTZ,
    action_started_at TIMESTAMPTZ,
    submitted_at TIMESTAMPTZ NOT NULL,
    result BOOLEAN NOT NULL,

    CONSTRAINT fk_attempt_game_session
        FOREIGN KEY (game_session_id)
        REFERENCES public.game_sessions(game_session_id)
        ON DELETE CASCADE,

    CONSTRAINT fk_attempt_question
        FOREIGN KEY (question_id)
        REFERENCES public.questions(question_id),

    CONSTRAINT uq_attempt
        UNIQUE (
            game_session_id,
            question_id,
            attempt_no
        ),

    CONSTRAINT chk_attempt_no
        CHECK (attempt_no > 0),

    CONSTRAINT chk_attempt_times
        CHECK (
            (
                action_started_at IS NULL
                OR prompt_finished_at IS NULL
                OR action_started_at >= prompt_finished_at
            )
            AND
            (
                action_started_at IS NULL
                OR submitted_at >= action_started_at
            )
        )
);