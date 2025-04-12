$(function () {
    // Initialize DataTables
    $('.datatable').DataTable({
        "paging": true,
        "lengthChange": false,
        "searching": true,
        "ordering": true,
        "info": true,
        "autoWidth": false,
        "responsive": true,
    });

    // Sidebar toggle
    $('[data-widget="pushmenu"]').PushMenu('init');
});