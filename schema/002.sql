CREATE TABLE IF NOT EXISTS User (
    UID TEXT PRIMARY KEY,
    Username TEXT NOT NULL,
    PassHash TEXT NOT NULL,
    Email TEXT,
    LastLogin TEXT
)