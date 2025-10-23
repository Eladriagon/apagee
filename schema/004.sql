-- Create index on Slug for Article
CREATE INDEX IF NOT EXISTS IX_Article_Slug ON Article (Slug);

-- Settings table
CREATE TABLE IF NOT EXISTS Settings (
    ID INT PRIMARY KEY,
    ThemeCss TEXT,
    EnableFontAwesome BIT,
    Favicon TEXT,
    AuthorAvatar TEXT
)