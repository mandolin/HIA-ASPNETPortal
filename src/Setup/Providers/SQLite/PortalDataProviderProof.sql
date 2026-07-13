DROP TABLE IF EXISTS PortalDataProviderProof;

CREATE TABLE PortalDataProviderProof
(
    ProofId INTEGER PRIMARY KEY AUTOINCREMENT,
    ProofKey TEXT NOT NULL UNIQUE,
    RecordedUtc TEXT NOT NULL,
    Note TEXT NULL
);
