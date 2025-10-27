CREATE TABLE IF NOT EXISTS APubFollowers (
    UID TEXT PRIMARY KEY,
    ID TEXT UNIQUE NOT NULL,
    FollowerId TEXT,
    FollowerName TEXT,
    CreatedOn TEXT NOT NULL
);

CREATE INDEX IX_APubFollowers_ID ON APubFollowers (ID);
CREATE INDEX IX_APubFollowers_FollowerId ON APubFollowers (FollowerId);
