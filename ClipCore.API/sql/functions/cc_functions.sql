-- ============================================================
-- ClipCore PostgreSQL Functions
-- Deploy this file before running the API.
-- Naming: cc_s_ = SELECT, cc_i_ = INSERT, cc_u_ = UPDATE, cc_d_ = DELETE
-- All tables/columns are PascalCase (no snake_case convention).
-- ============================================================

-- ─────────────────────────────────────────────────────────────
-- SELLERS
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION cc_s_seller_profile(p_seller_id integer)
RETURNS TABLE (
    "Id" integer, "UserId" text, "IsTrusted" boolean, "CreatedAt" timestamptz,
    "Email" text, "Slug" text, "DisplayName" text, "LogoUrl" text,
    "BannerUrl" text, "AccentColor" text, "Bio" text, "IsPublished" boolean
) AS $$
BEGIN
    RETURN QUERY
    SELECT s."Id", s."UserId", s."IsTrusted", s."CreatedAt",
           u."Email",
           sf."Slug", sf."DisplayName", sf."LogoUrl", sf."BannerUrl",
           sf."AccentColor", sf."Bio", sf."IsPublished"
    FROM "Sellers" s
    JOIN "AspNetUsers" u  ON u."Id"        = s."UserId"
    JOIN "Storefronts" sf ON sf."SellerId" = s."Id"
    WHERE s."Id" = p_seller_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_seller_profile_by_user(p_user_id text)
RETURNS TABLE (
    "Id" integer, "UserId" text, "IsTrusted" boolean, "CreatedAt" timestamptz,
    "Email" text, "Slug" text, "DisplayName" text, "LogoUrl" text,
    "BannerUrl" text, "AccentColor" text, "Bio" text, "IsPublished" boolean
) AS $$
BEGIN
    RETURN QUERY
    SELECT s."Id", s."UserId", s."IsTrusted", s."CreatedAt",
           u."Email",
           sf."Slug", sf."DisplayName", sf."LogoUrl", sf."BannerUrl",
           sf."AccentColor", sf."Bio", sf."IsPublished"
    FROM "Sellers" s
    JOIN "AspNetUsers" u  ON u."Id"        = s."UserId"
    JOIN "Storefronts" sf ON sf."SellerId" = s."Id"
    WHERE s."UserId" = p_user_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_i_seller(p_user_id text)
RETURNS integer AS $$
DECLARE v_id integer;
BEGIN
    INSERT INTO "Sellers" ("UserId", "IsTrusted", "CreatedAt")
    VALUES (p_user_id, false, NOW()) RETURNING "Id" INTO v_id;
    RETURN v_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE cc_i_storefront(p_seller_id integer, p_slug text, p_display_name text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO "Storefronts" ("SellerId", "Slug", "DisplayName", "IsPublished", "CreatedAt")
    VALUES (p_seller_id, p_slug, p_display_name, false, NOW());
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_storefront_settings(
    p_seller_id integer, p_display_name text, p_logo_url text,
    p_banner_url text, p_accent_color text, p_bio text, p_is_published boolean
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Storefronts" SET
        "DisplayName"  = p_display_name,
        "LogoUrl"      = p_logo_url,
        "BannerUrl"    = p_banner_url,
        "AccentColor"  = p_accent_color,
        "Bio"          = p_bio,
        "IsPublished"  = p_is_published
    WHERE "SellerId" = p_seller_id;
END;
$$;

CREATE OR REPLACE FUNCTION cc_s_seller_sales_stats(p_seller_id integer)
RETURNS TABLE (
    "TotalSales" integer, "TotalRevenueCents" bigint,
    "TotalPayoutCents" bigint, "PendingFulfillment" integer
) AS $$
BEGIN
    RETURN QUERY
    SELECT COUNT(*)::integer,
           COALESCE(SUM("PricePaidCents"), 0),
           COALESCE(SUM("SellerPayoutCents"), 0),
           COUNT(*) FILTER (WHERE "FulfillmentStatus" = 0)::integer
    FROM "Purchases" WHERE "SellerId" = p_seller_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_slug_exists(p_slug text)
RETURNS boolean AS $$
BEGIN
    RETURN EXISTS (SELECT 1 FROM "Storefronts" WHERE "Slug" = p_slug);
END;
$$ LANGUAGE plpgsql;

-- ─────────────────────────────────────────────────────────────
-- CLIPS
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION cc_s_clips_by_seller(p_seller_id integer)
RETURNS TABLE (
    "Id" text, "Title" text, "CollectionId" text, "CollectionName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer,
    "DurationSec" double precision, "PlaybackIdTeaser" text,
    "ThumbnailFileName" text, "IsArchived" boolean, "PublishedAt" timestamptz
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."CollectionId", col."Name",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", c."PlaybackIdTeaser", c."ThumbnailFileName",
           c."IsArchived", c."PublishedAt"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id" = c."CollectionId"
    WHERE c."SellerId" = p_seller_id
    ORDER BY c."PublishedAt" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_clips_by_collection(p_collection_id text)
RETURNS TABLE (
    "Id" text, "Title" text, "CollectionId" text, "CollectionName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer,
    "DurationSec" double precision, "PlaybackIdTeaser" text,
    "ThumbnailFileName" text, "IsArchived" boolean, "PublishedAt" timestamptz
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."CollectionId", col."Name",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", c."PlaybackIdTeaser", c."ThumbnailFileName",
           c."IsArchived", c."PublishedAt"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id" = c."CollectionId"
    WHERE c."CollectionId" = p_collection_id AND c."IsArchived" = false
    ORDER BY c."PublishedAt" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_clip_detail(p_clip_id text)
RETURNS TABLE (
    "Id" text, "Title" text, "CollectionId" text, "CollectionName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer,
    "DurationSec" double precision, "PlaybackIdSigned" text, "PlaybackIdTeaser" text,
    "MuxAssetId" text, "MuxUploadId" text, "MasterFileName" text,
    "ThumbnailFileName" text, "TagsJson" text, "RecordingStartedAt" timestamptz,
    "Width" integer, "Height" integer, "IsArchived" boolean,
    "ArchivedAt" timestamptz, "LastSoldAt" timestamptz, "PublishedAt" timestamptz,
    "SellerId" integer
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."CollectionId", col."Name",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", c."PlaybackIdSigned", c."PlaybackIdTeaser",
           c."MuxAssetId", c."MuxUploadId", c."MasterFileName", c."ThumbnailFileName",
           c."TagsJson", c."RecordingStartedAt", c."Width", c."Height",
           c."IsArchived", c."ArchivedAt", c."LastSoldAt", c."PublishedAt",
           c."SellerId"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id" = c."CollectionId"
    WHERE c."Id" = p_clip_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_clips_search(p_query text)
RETURNS TABLE (
    "Id" text, "Title" text, "CollectionId" text, "CollectionName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer,
    "DurationSec" double precision, "PlaybackIdTeaser" text,
    "ThumbnailFileName" text, "IsArchived" boolean, "PublishedAt" timestamptz
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."CollectionId", col."Name",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", c."PlaybackIdTeaser", c."ThumbnailFileName",
           c."IsArchived", c."PublishedAt"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id" = c."CollectionId"
    WHERE c."IsArchived" = false
      AND (c."Title" ILIKE '%' || p_query || '%' OR c."TagsJson" ILIKE '%' || p_query || '%')
    ORDER BY c."RecordingStartedAt" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_i_clip(
    p_id text, p_collection_id text, p_seller_id integer, p_title text,
    p_price_cents integer, p_price_commercial_cents integer,
    p_allow_gif_sale boolean, p_gif_price_cents integer, p_tags_json text
) RETURNS void AS $$
BEGIN
    INSERT INTO "Clips"
        ("Id", "CollectionId", "SellerId", "Title", "PriceCents", "PriceCommercialCents",
         "AllowGifSale", "GifPriceCents", "TagsJson", "PlaybackIdSigned", "IsArchived", "PublishedAt")
    VALUES
        (p_id, p_collection_id, p_seller_id, p_title, p_price_cents, p_price_commercial_cents,
         p_allow_gif_sale, p_gif_price_cents, p_tags_json, '', false, NOW());
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE cc_u_clip(
    p_clip_id text, p_seller_id integer, p_title text,
    p_price_cents integer, p_price_commercial_cents integer,
    p_allow_gif_sale boolean, p_gif_price_cents integer, p_tags_json text
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET
        "Title"                = p_title,
        "PriceCents"           = p_price_cents,
        "PriceCommercialCents" = p_price_commercial_cents,
        "AllowGifSale"         = p_allow_gif_sale,
        "GifPriceCents"        = p_gif_price_cents,
        "TagsJson"             = p_tags_json
    WHERE "Id" = p_clip_id AND "SellerId" = p_seller_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_clip_batch_settings(
    p_collection_id text, p_seller_id integer, p_price_cents integer,
    p_price_commercial_cents integer, p_allow_gif boolean, p_gif_price_cents integer
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET
        "PriceCents"           = p_price_cents,
        "PriceCommercialCents" = p_price_commercial_cents,
        "AllowGifSale"         = p_allow_gif,
        "GifPriceCents"        = p_gif_price_cents
    WHERE "CollectionId" = p_collection_id AND "SellerId" = p_seller_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_clip_mux_data(
    p_clip_id text, p_mux_asset_id text, p_playback_signed text,
    p_playback_teaser text, p_duration_sec double precision,
    p_width integer, p_height integer
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET
        "MuxAssetId"       = p_mux_asset_id,
        "PlaybackIdSigned" = p_playback_signed,
        "PlaybackIdTeaser" = p_playback_teaser,
        "DurationSec"      = p_duration_sec,
        "Width"            = p_width,
        "Height"           = p_height
    WHERE "Id" = p_clip_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_clip_mux_upload_id(p_clip_id text, p_upload_id text)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET "MuxUploadId" = p_upload_id WHERE "Id" = p_clip_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_clip_archive(p_clip_id text)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET
        "IsArchived"       = true,
        "ArchivedAt"       = NOW(),
        "PlaybackIdSigned" = NULL,
        "MuxAssetId"       = NULL
    WHERE "Id" = p_clip_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_clip_last_sold(p_clip_id text)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Clips" SET "LastSoldAt" = NOW() WHERE "Id" = p_clip_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_d_clip(p_clip_id text, p_seller_id integer)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM "Clips" WHERE "Id" = p_clip_id AND "SellerId" = p_seller_id;
END;
$$;

CREATE OR REPLACE FUNCTION cc_s_archive_candidates(p_days integer)
RETURNS TABLE ("Id" text, "MuxAssetId" text) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."MuxAssetId" FROM "Clips" c
    WHERE c."IsArchived" = false
      AND c."MuxAssetId" IS NOT NULL
      AND c."MuxAssetId" NOT LIKE 'errored:%'
      AND (
        (c."LastSoldAt" IS NULL     AND c."PublishedAt" < NOW() - (p_days || ' days')::interval)
        OR
        (c."LastSoldAt" IS NOT NULL AND c."LastSoldAt"  < NOW() - (p_days || ' days')::interval)
      );
END;
$$ LANGUAGE plpgsql;

-- ─────────────────────────────────────────────────────────────
-- COLLECTIONS
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION cc_s_collections_by_seller(p_seller_id integer)
RETURNS TABLE (
    "Id" text, "Name" text, "Date" date, "Location" text, "Summary" text,
    "DefaultPriceCents" integer, "DefaultPriceCommercialCents" integer,
    "DefaultAllowGifSale" boolean, "DefaultGifPriceCents" integer,
    "HeroClipId" text, "CreatedAt" timestamptz, "ClipCount" integer
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Name", c."Date", c."Location", c."Summary",
           c."DefaultPriceCents", c."DefaultPriceCommercialCents",
           c."DefaultAllowGifSale", c."DefaultGifPriceCents",
           c."HeroClipId", c."CreatedAt", COUNT(cl."Id")::integer
    FROM "Collections" c
    LEFT JOIN "Clips" cl ON cl."CollectionId" = c."Id" AND cl."IsArchived" = false
    WHERE c."SellerId" = p_seller_id
    GROUP BY c."Id" ORDER BY c."Date" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_i_collection(
    p_id text, p_seller_id integer, p_name text, p_date date,
    p_location text, p_summary text, p_default_price_cents integer,
    p_default_price_commercial_cents integer, p_default_allow_gif_sale boolean,
    p_default_gif_price_cents integer
) RETURNS void AS $$
BEGIN
    INSERT INTO "Collections"
        ("Id", "SellerId", "Name", "Date", "Location", "Summary",
         "DefaultPriceCents", "DefaultPriceCommercialCents",
         "DefaultAllowGifSale", "DefaultGifPriceCents", "CreatedAt")
    VALUES
        (p_id, p_seller_id, p_name, p_date, p_location, p_summary,
         p_default_price_cents, p_default_price_commercial_cents,
         p_default_allow_gif_sale, p_default_gif_price_cents, NOW());
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE cc_u_collection(
    p_collection_id text, p_seller_id integer, p_name text, p_date date,
    p_location text, p_summary text, p_default_price_cents integer,
    p_default_price_commercial_cents integer, p_default_allow_gif_sale boolean,
    p_default_gif_price_cents integer, p_hero_clip_id text
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Collections" SET
        "Name"                        = p_name,
        "Date"                        = p_date,
        "Location"                    = p_location,
        "Summary"                     = p_summary,
        "DefaultPriceCents"           = p_default_price_cents,
        "DefaultPriceCommercialCents" = p_default_price_commercial_cents,
        "DefaultAllowGifSale"         = p_default_allow_gif_sale,
        "DefaultGifPriceCents"        = p_default_gif_price_cents,
        "HeroClipId"                  = p_hero_clip_id
    WHERE "Id" = p_collection_id AND "SellerId" = p_seller_id;
END;
$$;

CREATE OR REPLACE PROCEDURE cc_d_collection(p_collection_id text, p_seller_id integer)
LANGUAGE plpgsql AS $$
BEGIN
    DELETE FROM "Collections" WHERE "Id" = p_collection_id AND "SellerId" = p_seller_id;
END;
$$;

-- Returns clip asset references for pre-deletion cleanup (Mux + R2)
CREATE OR REPLACE FUNCTION cc_s_collection_clip_assets(p_collection_id text, p_seller_id integer)
RETURNS TABLE ("Id" text, "MuxAssetId" text, "ThumbnailFileName" text, "MasterFileName" text) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."MuxAssetId", c."ThumbnailFileName", c."MasterFileName"
    FROM "Clips" c
    WHERE c."CollectionId" = p_collection_id AND c."SellerId" = p_seller_id;
END;
$$ LANGUAGE plpgsql;

-- ─────────────────────────────────────────────────────────────
-- PURCHASES
-- ─────────────────────────────────────────────────────────────

-- Snapshot of clip data at time of purchase (collection name, date, etc.)
CREATE OR REPLACE FUNCTION cc_s_purchase_snapshot(p_clip_id text)
RETURNS TABLE (
    "ClipTitle" text, "CollectionName" text, "CollectionDate" date,
    "RecordingStartedAt" timestamptz, "DurationSec" double precision,
    "MasterFileName" text, "ThumbnailFileName" text
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Title", col."Name", col."Date",
           c."RecordingStartedAt", c."DurationSec",
           c."MasterFileName", c."ThumbnailFileName"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id" = c."CollectionId"
    WHERE c."Id" = p_clip_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_i_purchase(
    p_user_id text, p_clip_id text, p_seller_id integer,
    p_clip_title text, p_collection_name text, p_collection_date date,
    p_recording_started_at timestamptz, p_duration_sec double precision,
    p_master_file_name text, p_thumbnail_file_name text,
    p_stripe_session_id text, p_order_id text,
    p_price_paid_cents integer, p_platform_fee_cents integer, p_seller_payout_cents integer,
    p_license_type integer, p_customer_email text, p_customer_name text,
    p_is_gif boolean, p_gif_start_time double precision, p_gif_end_time double precision
) RETURNS integer AS $$
DECLARE v_id integer;
BEGIN
    INSERT INTO "Purchases"
        ("UserId", "ClipId", "SellerId", "ClipTitle", "CollectionName", "CollectionDate",
         "ClipRecordingStartedAt", "ClipDurationSec", "ClipMasterFileName", "ClipThumbnailFileName",
         "StripeSessionId", "OrderId", "PricePaidCents", "PlatformFeeCents", "SellerPayoutCents",
         "LicenseType", "CustomerEmail", "CustomerName", "IsGif", "GifStartTime", "GifEndTime",
         "FulfillmentStatus", "CreatedAt")
    VALUES
        (p_user_id, p_clip_id, p_seller_id, p_clip_title, p_collection_name, p_collection_date,
         p_recording_started_at, p_duration_sec, p_master_file_name, p_thumbnail_file_name,
         p_stripe_session_id, p_order_id, p_price_paid_cents, p_platform_fee_cents, p_seller_payout_cents,
         p_license_type, p_customer_email, p_customer_name, p_is_gif, p_gif_start_time, p_gif_end_time,
         0, NOW())
    RETURNING "Id" INTO v_id;
    RETURN v_id;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE cc_u_purchase_fulfill(
    p_purchase_id integer, p_high_res_download_url text, p_mux_asset_id text
)
LANGUAGE plpgsql AS $$
BEGIN
    UPDATE "Purchases" SET
        "FulfillmentStatus"    = 1,
        "HighResDownloadUrl"   = p_high_res_download_url,
        "FulfillmentMuxAssetId" = p_mux_asset_id,
        "FulfilledAt"          = NOW()
    WHERE "Id" = p_purchase_id;
END;
$$;

CREATE OR REPLACE FUNCTION cc_s_seller_sales_summary()
RETURNS TABLE (
    "SellerId" integer, "DisplayName" text, "Slug" text,
    "SalesCount" bigint, "TotalRevenueCents" bigint,
    "PlatformFeeCents" bigint, "SellerPayoutCents" bigint
) AS $$
BEGIN
    RETURN QUERY
    SELECT p."SellerId", sf."DisplayName", sf."Slug",
           COUNT(p."Id"), SUM(p."PricePaidCents"),
           SUM(p."PlatformFeeCents"), SUM(p."SellerPayoutCents")
    FROM "Purchases" p
    JOIN "Storefronts" sf ON sf."SellerId" = p."SellerId"
    WHERE p."SellerId" IS NOT NULL
    GROUP BY p."SellerId", sf."DisplayName", sf."Slug"
    ORDER BY SUM(p."PricePaidCents") DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_daily_revenue(p_days integer)
RETURNS TABLE ("Date" date, "TotalCents" bigint) AS $$
BEGIN
    RETURN QUERY
    SELECT ("CreatedAt"::date), SUM("PricePaidCents")
    FROM "Purchases"
    WHERE "CreatedAt" >= NOW() - (p_days || ' days')::interval
    GROUP BY ("CreatedAt"::date)
    ORDER BY ("CreatedAt"::date);
END;
$$ LANGUAGE plpgsql;

-- ─────────────────────────────────────────────────────────────
-- MARKETPLACE
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION cc_s_storefront(p_slug text)
RETURNS TABLE (
    "Slug" text, "DisplayName" text, "LogoUrl" text, "BannerUrl" text,
    "AccentColor" text, "Bio" text, "IsTrusted" boolean
) AS $$
BEGIN
    RETURN QUERY
    SELECT sf."Slug", sf."DisplayName", sf."LogoUrl", sf."BannerUrl",
           sf."AccentColor", sf."Bio", s."IsTrusted"
    FROM "Storefronts" sf
    JOIN "Sellers" s ON s."Id" = sf."SellerId"
    WHERE sf."Slug" = p_slug AND sf."IsPublished" = true AND s."IsTrusted" = true;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_storefront_clips(p_slug text)
RETURNS TABLE (
    "Id" text, "Title" text, "PlaybackIdTeaser" text, "ThumbnailFileName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer, "DurationSec" double precision,
    "CollectionName" text, "StorefrontSlug" text
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."PlaybackIdTeaser", c."ThumbnailFileName",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", col."Name", sf."Slug"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id"      = c."CollectionId"
    JOIN "Storefronts"  sf  ON sf."SellerId" = c."SellerId"
    WHERE c."SellerId" = (SELECT sf2."SellerId" FROM "Storefronts" sf2 WHERE sf2."Slug" = p_slug)
      AND c."IsArchived" = false AND c."PlaybackIdTeaser" IS NOT NULL
    ORDER BY c."PublishedAt" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_marketplace_clips(p_search_term text, p_page_size integer, p_offset integer)
RETURNS TABLE (
    "Id" text, "Title" text, "PlaybackIdTeaser" text, "ThumbnailFileName" text,
    "PriceCents" integer, "PriceCommercialCents" integer,
    "AllowGifSale" boolean, "GifPriceCents" integer, "DurationSec" double precision,
    "CollectionName" text, "StorefrontSlug" text
) AS $$
BEGIN
    RETURN QUERY
    SELECT c."Id", c."Title", c."PlaybackIdTeaser", c."ThumbnailFileName",
           c."PriceCents", c."PriceCommercialCents", c."AllowGifSale", c."GifPriceCents",
           c."DurationSec", col."Name", sf."Slug"
    FROM "Clips" c
    JOIN "Collections" col ON col."Id"   = c."CollectionId"
    JOIN "Storefronts"  sf  ON sf."SellerId" = c."SellerId"
    JOIN "Sellers"      s   ON s."Id"    = c."SellerId"
    WHERE c."IsArchived" = false AND c."PlaybackIdTeaser" IS NOT NULL
      AND s."IsTrusted" = true
      AND (p_search_term IS NULL OR c."Title" ILIKE '%' || p_search_term || '%')
    ORDER BY c."PublishedAt" DESC
    LIMIT p_page_size OFFSET p_offset;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE FUNCTION cc_s_marketplace_clips_count(p_search_term text)
RETURNS integer AS $$
DECLARE v_count integer;
BEGIN
    SELECT COUNT(c."Id")::integer INTO v_count
    FROM "Clips" c
    JOIN "Sellers" s ON s."Id" = c."SellerId"
    WHERE c."IsArchived" = false AND c."PlaybackIdTeaser" IS NOT NULL
      AND s."IsTrusted" = true
      AND (p_search_term IS NULL OR c."Title" ILIKE '%' || p_search_term || '%');
    RETURN v_count;
END;
$$ LANGUAGE plpgsql;

-- ─────────────────────────────────────────────────────────────
-- ADMIN
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE FUNCTION cc_s_admin_sellers()
RETURNS TABLE (
    "Id" integer, "Email" text, "DisplayName" text, "Slug" text,
    "IsTrusted" boolean, "IsPublished" boolean, "CreatedAt" timestamptz,
    "ClipCount" integer, "SalesCount" integer
) AS $$
BEGIN
    RETURN QUERY
    SELECT s."Id", u."Email", sf."DisplayName", sf."Slug",
           s."IsTrusted", sf."IsPublished", s."CreatedAt",
           COUNT(DISTINCT c."Id")::integer, COUNT(DISTINCT p."Id")::integer
    FROM "Sellers" s
    JOIN "AspNetUsers"  u   ON u."Id"        = s."UserId"
    JOIN "Storefronts"  sf  ON sf."SellerId" = s."Id"
    LEFT JOIN "Clips"   c   ON c."SellerId"  = s."Id"
    LEFT JOIN "Purchases" p ON p."SellerId"  = s."Id"
    GROUP BY s."Id", u."Email", sf."DisplayName", sf."Slug",
             s."IsTrusted", sf."IsPublished", s."CreatedAt"
    ORDER BY s."CreatedAt" DESC;
END;
$$ LANGUAGE plpgsql;

CREATE OR REPLACE PROCEDURE cc_u_seller_approve(p_seller_id integer)
LANGUAGE plpgsql AS $$
BEGIN UPDATE "Sellers" SET "IsTrusted" = true  WHERE "Id" = p_seller_id; END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_seller_revoke(p_seller_id integer)
LANGUAGE plpgsql AS $$
BEGIN UPDATE "Sellers" SET "IsTrusted" = false WHERE "Id" = p_seller_id; END;
$$;

CREATE OR REPLACE FUNCTION cc_s_platform_stats()
RETURNS TABLE (
    "TotalSellers" integer, "TrustedSellers" integer, "TotalClips" integer,
    "TotalPurchases" integer, "TotalRevenueCents" bigint,
    "TotalPlatformFees" bigint, "TotalPayouts" bigint
) AS $$
BEGIN
    RETURN QUERY
    SELECT
        (SELECT COUNT(*)::integer FROM "Sellers"),
        (SELECT COUNT(*)::integer FROM "Sellers"   WHERE "IsTrusted" = true),
        (SELECT COUNT(*)::integer FROM "Clips"     WHERE "IsArchived" = false),
        (SELECT COUNT(*)::integer FROM "Purchases"),
        (SELECT COALESCE(SUM("PricePaidCents"),    0) FROM "Purchases"),
        (SELECT COALESCE(SUM("PlatformFeeCents"),  0) FROM "Purchases"),
        (SELECT COALESCE(SUM("SellerPayoutCents"), 0) FROM "Purchases");
END;
$$ LANGUAGE plpgsql;

-- ─────────────────────────────────────────────────────────────
-- SETTINGS + USAGE
-- ─────────────────────────────────────────────────────────────

CREATE OR REPLACE PROCEDURE cc_u_setting(p_key text, p_value text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO "Settings" ("Key", "Value", "UpdatedAt")
    VALUES (p_key, p_value, NOW())
    ON CONFLICT ("Key") DO UPDATE
    SET "Value" = EXCLUDED."Value", "UpdatedAt" = NOW();
END;
$$;

CREATE OR REPLACE PROCEDURE cc_u_usage_increment(p_ip_address text, p_date date, p_user_id text)
LANGUAGE plpgsql AS $$
BEGIN
    INSERT INTO "DailyWatchUsages" ("IpAddress", "Date", "UserId", "TokenRequestCount")
    VALUES (p_ip_address, p_date, p_user_id, 1)
    ON CONFLICT ("IpAddress", "Date") DO UPDATE
    SET "TokenRequestCount" = "DailyWatchUsages"."TokenRequestCount" + 1,
        "UserId" = COALESCE("DailyWatchUsages"."UserId", EXCLUDED."UserId");
END;
$$;

-- ─────────────────────────────────────────────────────────────
-- POST-DEPLOY NOTES
-- ─────────────────────────────────────────────────────────────
-- Required unique constraint for cc_u_usage_increment UPSERT:
-- ALTER TABLE "DailyWatchUsages" ADD UNIQUE ("IpAddress", "Date");
-- (skip if constraint already exists)
--
-- Verify all functions deployed:
-- SELECT routine_name FROM information_schema.routines
-- WHERE routine_schema = 'public' ORDER BY routine_name;
