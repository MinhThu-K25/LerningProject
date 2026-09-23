using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace QuanLyThuVien.Migrations
{
    /// <inheritdoc />
    public partial class AddCMNDToThanhVien : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhieuMuonChiTiets_PhieuMuon_BorrowRecordBorrowId",
                table: "PhieuMuonChiTiets");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuPhats_Members_MemberId",
                table: "PhieuPhats");

            migrationBuilder.DropIndex(
                name: "IX_PhieuMuonChiTiets_BorrowRecordBorrowId",
                table: "PhieuMuonChiTiets");

            migrationBuilder.DropColumn(
                name: "BorrowRecordBorrowId",
                table: "PhieuMuonChiTiets");

            migrationBuilder.DropColumn(
                name: "RoleName",
                table: "Members");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "MemberCode",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddColumn<string>(
                name: "CMND",
                table: "Members",
                type: "nvarchar(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuMuonChiTiets_BorrowId",
                table: "PhieuMuonChiTiets",
                column: "BorrowId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuMuonChiTiets_PhieuMuon_BorrowId",
                table: "PhieuMuonChiTiets",
                column: "BorrowId",
                principalTable: "PhieuMuon",
                principalColumn: "BorrowId",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuPhats_Members_MemberId",
                table: "PhieuPhats",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PhieuMuonChiTiets_PhieuMuon_BorrowId",
                table: "PhieuMuonChiTiets");

            migrationBuilder.DropForeignKey(
                name: "FK_PhieuPhats_Members_MemberId",
                table: "PhieuPhats");

            migrationBuilder.DropIndex(
                name: "IX_PhieuMuonChiTiets_BorrowId",
                table: "PhieuMuonChiTiets");

            migrationBuilder.DropColumn(
                name: "CMND",
                table: "Members");

            migrationBuilder.AddColumn<int>(
                name: "BorrowRecordBorrowId",
                table: "PhieuMuonChiTiets",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Members",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MemberCode",
                table: "Members",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RoleName",
                table: "Members",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PhieuMuonChiTiets_BorrowRecordBorrowId",
                table: "PhieuMuonChiTiets",
                column: "BorrowRecordBorrowId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuMuonChiTiets_PhieuMuon_BorrowRecordBorrowId",
                table: "PhieuMuonChiTiets",
                column: "BorrowRecordBorrowId",
                principalTable: "PhieuMuon",
                principalColumn: "BorrowId");

            migrationBuilder.AddForeignKey(
                name: "FK_PhieuPhats_Members_MemberId",
                table: "PhieuPhats",
                column: "MemberId",
                principalTable: "Members",
                principalColumn: "MemberId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
