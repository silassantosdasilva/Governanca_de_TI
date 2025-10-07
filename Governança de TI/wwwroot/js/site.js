// Aguarda o DOM ser completamente carregado para executar o script

/*Sidebar Inicio*/
document.addEventListener("DOMContentLoaded", function () {

    // Seleciona o botão de toggle e a sidebar
    const sidebarToggle = document.getElementById('sidebar-toggle');
    const sidebar = document.getElementById('sidebar');
    const DesativaPadding = document.getElementById('Img_Logo_Menu');

    // Verifica se os elementos existem
    if (sidebarToggle && sidebar) {

        // Adiciona o evento de clique
        sidebarToggle.addEventListener('click', function (e) {
            e.preventDefault(); // Previne o comportamento padrão do link

            // Adiciona ou remove a classe 'expanded'
           
            sidebar.classList.toggle('expanded');
        });
    }
});

 
/*SideBar Fim*/