CREATE TABLE IF NOT EXISTS Interaction (
    ID TEXT PRIMARY KEY,
    ArticleUID TEXT,
    Type INT
);

CREATE INDEX IF NOT EXISTS IX_Interaction_ArticleUID_Type ON Interaction (ArticleUID, Type);