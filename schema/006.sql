ALTER TABLE Settings ADD COLUMN ShowBioOnSite BIT;

CREATE TABLE IF NOT EXISTS Inbox (
    ID TEXT PRIMARY KEY,
    UID TEXT NOT NULL UNIQUE,
    Type TEXT,
    ContentType TEXT,
    RemoteServer TEXT,
    ReceivedOn TEXT NOT NULL,
    BodySize INT NOT NULL,
    Body BLOB
)