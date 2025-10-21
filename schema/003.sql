CREATE TABLE IF NOT EXISTS Article (
    UID TEXT PRIMARY KEY,
    Slug TEXT UNIQUE NOT NULL,
    CreatedOn TEXT NOT NULL,
    PublishedOn TEXT,
    Status INT NOT NULL,
    Title TEXT NOT NULL,
    Body TEXT,
    BodyMode INT NOT NULL
)