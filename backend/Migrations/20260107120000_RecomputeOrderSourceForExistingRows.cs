using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InnriGreifi.API.Migrations
{
    /// <inheritdoc />
    public partial class RecomputeOrderSourceForExistingRows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Recompute OrderSource for all existing rows based on OrderNumber
            // Logic: Extract digits from OrderNumber, parse as integer
            // If < 1000: "Counter", if >= 1000: "Web", otherwise: "Unknown"
            migrationBuilder.Sql("""
                DO $$
                DECLARE
                    rec RECORD;
                    digits TEXT;
                    order_num INTEGER;
                BEGIN
                    FOR rec IN SELECT "Id", "OrderNumber" FROM "OrderRows" WHERE "OrderSource" = 'Unknown' OR "OrderSource" IS NULL
                    LOOP
                        -- Extract digits from OrderNumber
                        digits := REGEXP_REPLACE(COALESCE(rec."OrderNumber", ''), '[^0-9]', '', 'g');
                        
                        -- Try to parse as integer
                        BEGIN
                            IF digits = '' THEN
                                order_num := -1;
                            ELSE
                                order_num := digits::INTEGER;
                            END IF;
                            
                            -- Update based on order number
                            IF order_num < 0 THEN
                                UPDATE "OrderRows" SET "OrderSource" = 'Unknown' WHERE "Id" = rec."Id";
                            ELSIF order_num < 1000 THEN
                                UPDATE "OrderRows" SET "OrderSource" = 'Counter' WHERE "Id" = rec."Id";
                            ELSE
                                UPDATE "OrderRows" SET "OrderSource" = 'Web' WHERE "Id" = rec."Id";
                            END IF;
                        EXCEPTION WHEN OTHERS THEN
                            -- If parsing fails, set to Unknown
                            UPDATE "OrderRows" SET "OrderSource" = 'Unknown' WHERE "Id" = rec."Id";
                        END;
                    END LOOP;
                END $$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No need to revert - this is a data fix
        }
    }
}

