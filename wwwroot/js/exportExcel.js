const exportExcel = (idTable, title, fileName, from_date, to_date) => {
    // helper: chuyển số cột -> chữ (1->A, 2->B...)
    function colLetter(n) {
        let s = '';
        while (n > 0) { const m = (n - 1) % 26; s = String.fromCharCode(65 + m) + s; n = Math.floor((n - 1) / 26); }
        return s;
    }

    function expandRow($tr, colCount) {
        const out = [];
        $tr.children().each(function () {
            const txt = $(this).text().trim();
            const cs = parseInt($(this).attr('colspan') || 1, 10);
            for (let i = 0; i < cs; i++) out.push(i === 0 ? txt : '');
        });
        while (out.length < colCount) out.push('');
        return out.slice(0, colCount);
    }

    $('#btn-export-excel').click(async function () {
        const $table = $(`#${idTable}`);
        if (!$table.length) { alert('Không tìm thấy bảng dữ liệu để xuất Excel.'); return; }

        // Lấy dữ liệu từ bảng HTML
        const headers = $table.find('thead tr').first().children().toArray().map(th => $(th).text().trim());
        const rows = $table.find('tbody tr').toArray().map(tr =>
            $(tr).children().toArray().map(td => $(td).text().trim())
        );
        const colCount = headers.length;


        // tfoot (nếu có)
        let foot = null;
        const $tfootTr = $table.find('tfoot tr').last();
        if ($tfootTr.length) foot = expandRow($tfootTr, colCount);

        // Ngày (Razor/JS)
        const fromDate = (typeof from_date !== 'undefined' && from_date) ? from_date : 'start';
        const toDate = (typeof to_date !== 'undefined' && to_date) ? to_date : 'end';

        const filename = `${fileName}_${fromDate}_den_${toDate}.xlsx`;

        // Tạo workbook/sheet
        const wb = new ExcelJS.Workbook();
        const ws = wb.addWorksheet('DoanhThu');

        // Tiêu đề + khoảng ngày
        const lastCol = colLetter(colCount);
        ws.mergeCells(`A1:${lastCol}1`);
        ws.mergeCells(`A2:${lastCol}2`);
        ws.getCell('A1').value = title;
        ws.getCell('A2').value = `Từ ${fromDate} đến ${toDate}`;
        ws.getRow(1).height = 30;

        // Style tiêu đề
        Object.assign(ws.getCell('A1'), {
            font: { size: 18, bold: true, color: { argb: 'FFFFFFFF' } },
            alignment: { horizontal: 'center', vertical: 'middle' },
            fill: { type: 'pattern', pattern: 'solid', fgColor: { argb: '4472C4' } }
        });
        Object.assign(ws.getCell('A2'), {
            font: { italic: true, color: { argb: '404040' } },
            alignment: { horizontal: 'center' }
        });

        // Header
        ws.addRow([]); // row 3 để trống cho đẹp
        ws.addRow(headers); // row 4 là header
        const headerRow = ws.getRow(4);
        headerRow.eachCell(c => {
            c.font = { bold: true, color: { argb: 'FFFFFFFF' } };
            c.alignment = { horizontal: 'center', vertical: 'middle' };
            c.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: '305496' } };
            c.border = { top: { style: 'thin' }, left: { style: 'thin' }, bottom: { style: 'thin' }, right: { style: 'thin' } };
        });

        // Dữ liệu
        rows.forEach(arr => ws.addRow(arr));

        let totalRowIndex = null;
        if (foot) {
            ws.addRow(foot);
            totalRowIndex = ws.lastRow.number;
            const totalRow = ws.getRow(totalRowIndex);
            totalRow.eachCell(c => {
                c.font = { bold: true, color: { argb: 'FF000000' } };
                c.alignment = { horizontal: 'right', vertical: 'middle', wrapText: true };
                c.fill = { type: 'pattern', pattern: 'solid', fgColor: { argb: 'FFF2F2F2' } };
                c.border = { top: { style: 'thin' }, left: { style: 'thin' }, bottom: { style: 'thin' }, right: { style: 'thin' } };
            });
        }

        // Border + căn giữa cho body
        ws.eachRow((row, i) => {
            if (i >= 5) {
                row.eachCell(cell => {
                    cell.alignment = { horizontal: 'center', vertical: 'middle', wrapText: true };
                    cell.border = { top: { style: 'thin' }, left: { style: 'thin' }, bottom: { style: 'thin' }, right: { style: 'thin' } };
                });
            }
        });

        // Auto width theo nội dung
        const lengths = Array(colCount).fill(0);
        ws.eachRow((row) => {
            row.eachCell({ includeEmpty: true }, (cell, colNumber) => {
                const text = cell.value == null ? '' : String(cell.value);
                const maxLine = text.split(/\r?\n/).reduce((m, line) => Math.max(m, line.length), 0);
                lengths[colNumber - 1] = Math.max(lengths[colNumber - 1], maxLine);
            });
        });

        ws.columns = lengths.map(len => ({ width: Math.min(Math.max(10, len + 2), 40) }));

        const buffer = await wb.xlsx.writeBuffer();
        saveAs(new Blob([buffer]), filename);
    });
}
