CREATE TABLE IF NOT EXISTS KeyValueStore (
    Key TEXT PRIMARY KEY,
    Value TEXT
);

ALTER TABLE Settings ADD COLUMN AutoReciprocateFollows BIT;