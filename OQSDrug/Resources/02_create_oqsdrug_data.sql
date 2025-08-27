-- まずDROP（存在すれば）
DROP TABLE IF EXISTS public.connectedclient CASCADE;
DROP TABLE IF EXISTS public.drug_history   CASCADE;
DROP TABLE IF EXISTS public.reqresults     CASCADE;
DROP TABLE IF EXISTS public.settings       CASCADE;
DROP TABLE IF EXISTS public.sinryo_history CASCADE;
DROP TABLE IF EXISTS public.tkk_history    CASCADE;
DROP TABLE IF EXISTS public.tkk_reference  CASCADE;

-- シーケンス（必要なら）
DROP SEQUENCE IF EXISTS public.connectedclient_id_seq CASCADE;
DROP SEQUENCE IF EXISTS public.drug_history_id_seq    CASCADE;
DROP SEQUENCE IF EXISTS public.reqresults_id_seq      CASCADE;
DROP SEQUENCE IF EXISTS public.settings_id_seq        CASCADE;
DROP SEQUENCE IF EXISTS public.sinryo_history_id_seq  CASCADE;
DROP SEQUENCE IF EXISTS public.tkk_history_id_seq     CASCADE;
DROP SEQUENCE IF EXISTS public.tkk_reference_id_seq   CASCADE;

-- =========================
-- シーケンス作成
-- =========================
CREATE SEQUENCE public.connectedclient_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
CREATE SEQUENCE public.drug_history_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
CREATE SEQUENCE public.reqresults_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
CREATE SEQUENCE public.settings_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
CREATE SEQUENCE public.sinryo_history_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
CREATE SEQUENCE public.tkk_history_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;
CREATE SEQUENCE public.tkk_reference_id_seq START WITH 1 INCREMENT BY 1 NO MINVALUE NO MAXVALUE CACHE 1;

-- =========================
-- テーブル作成
-- =========================
CREATE TABLE public.connectedclient (
    id integer NOT NULL DEFAULT nextval('public.connectedclient_id_seq'),
    clientname character varying(32),
    lastupdated timestamp without time zone,
    PRIMARY KEY (id)
);

CREATE TABLE public.drug_history (
    id integer NOT NULL DEFAULT nextval('public.drug_history_id_seq'),
    ptid integer,
    diorg integer,
    metrdihcd character varying(12),
    metrdihnm character varying(255),
    metrmonth character varying(8),
    prlshcd character varying(12),
    prlshnm character varying(255),
    prisorg integer,
    "inout" integer,
    didate character varying(10),
    prdate character varying(10),
    drugc character varying(12),
    qua1 double precision,
    usagen character varying(255),
    times integer,
    ingren character varying(255),
    metridcl integer,
    unit character varying(50),
    drugn character varying(255),
    ptname character varying(255),
    ptkana character varying(255),
    birth character varying(10),
    ptidmain integer,
    receivedate character varying(10),
    source integer,
    revised boolean NOT NULL DEFAULT FALSE,
    PRIMARY KEY (id)
);

CREATE TABLE public.reqresults (
    id integer NOT NULL DEFAULT nextval('public.reqresults_id_seq'),
    ptid integer,
    ptname character varying(255),
    category integer,
    reqdate timestamp without time zone,
    reqfile character varying(255),
    resfile character varying(255),
    resdate timestamp without time zone,
    result character varying(255),
    categoryname character varying(12),
    PRIMARY KEY (id)
);

CREATE TABLE public.settings (
    id integer NOT NULL DEFAULT nextval('public.settings_id_seq'),
    key character varying(255) NOT NULL,
    setting_value character varying(255),
    PRIMARY KEY (id),
    CONSTRAINT settings_key_unique UNIQUE (key) -- ON CONFLICT用制約
);

CREATE TABLE public.sinryo_history (
    id integer NOT NULL DEFAULT nextval('public.sinryo_history_id_seq'),
    ptid integer,
    ptidmain integer,
    ptname character varying(255),
    ptkana character varying(255),
    birth character varying(10),
    sex integer,
    metrdihcd character varying(12),
    metrdihnm character varying(255),
    metrmonth character varying(10),
    didate character varying(10),
    sininfn character varying(255),
    sininfcd character varying(12),
    metridcl character varying(12),
    qua1 double precision,
    times integer,
    unit character varying(50),
    receivedate character varying(10),
    PRIMARY KEY (id)
);

CREATE TABLE public.tkk_history (
    id integer NOT NULL DEFAULT nextval('public.tkk_history_id_seq'),
    effectivetime character varying(8),
    itemcode character varying(32),
    itemname character varying(128),
    datatype character varying(4),
    datavalue character varying(64),
    unit character varying(32),
    oid character varying(32),
    datavaluename character varying(64),
    ptidmain integer,
    ptname character varying(64),
    ptkana character varying(64),
    sex text,
    PRIMARY KEY (id)
);

CREATE TABLE public.tkk_reference (
    id integer NOT NULL DEFAULT nextval('public.tkk_reference_id_seq'),
    itemcode character varying(32),
    itemname character varying(128),
    sex integer,
    compairtype character varying(8),
    limit1 character varying(16),
    limit2 character varying(16),
    includevalue character varying(16),
    PRIMARY KEY (id)
);
