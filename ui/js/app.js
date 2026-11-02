// PDF Studio Light - Frontend Controller
document.addEventListener('DOMContentLoaded', () => {
  // Global state
  const state = {
    settings: {
      theme: 'Dark',
      output_directory: '',
      auto_open_output_folder: false
    },
    merge: {
      files: [],
      selectedIndex: -1
    },
    split: {
      filePath: null,
      pageCount: 0
    },
    edit: {
      filePath: null,
      pageCount: 0,
      pages: [], // array of { pageNum: 1, rotation: 0 }
      selectedIndex: -1
    },
    compress: {
      filePath: null,
      zipFiles: []
    },
    stamp: {
      pdfPath: null,
      imgPath: null,
      pageCount: 1
    },
    sign: {
      pdfPath: null,
      pageCount: 1,
      customImgPath: null
    },
    img2pdf: {
      files: [],
      selectedIndex: -1
    }
  };

  // UI Elements
  const statusBar = document.getElementById('status-bar');
  const statusText = document.getElementById('status-text');
  const spinner = document.getElementById('loading-spinner');

  // Modal Dialog
  const modal = document.getElementById('app-modal');
  const modalTitle = document.getElementById('modal-title');
  const modalContent = document.getElementById('modal-content');
  const modalBtnOk = document.getElementById('modal-btn-ok');

  function showModal(title, message) {
    modalTitle.textContent = title;
    modalContent.textContent = message;
    modal.classList.remove('hidden');
  }

  modalBtnOk.addEventListener('click', () => {
    modal.classList.add('hidden');
  });

  function setStatus(msg, isLoading = false) {
    statusText.textContent = msg;
    if (isLoading) {
      spinner.classList.remove('hidden');
    } else {
      spinner.classList.add('hidden');
    }
  }

  // Tauri Invoker
  async function invoke(cmd, args = {}) {
    if (window.__TAURI__ && window.__TAURI__.core) {
      return await window.__TAURI__.core.invoke(cmd, args);
    }
    throw new Error('Runtime de Tauri no detectado.');
  }

  async function openFileDialog(options) {
    if (window.__TAURI__ && window.__TAURI__.dialog) {
      return await window.__TAURI__.dialog.open(options);
    }
    return options.multiple ? [] : null;
  }

  async function saveFileDialog(options) {
    if (window.__TAURI__ && window.__TAURI__.dialog) {
      return await window.__TAURI__.dialog.save(options);
    }
    return null;
  }

  // Tab Navigation
  const navItems = document.querySelectorAll('.nav-item[data-tab]');
  const tabPanels = document.querySelectorAll('.tab-panel');

  navItems.forEach(item => {
    item.addEventListener('click', () => {
      const tabId = item.getAttribute('data-tab');
      navItems.forEach(n => n.classList.remove('active'));
      tabPanels.forEach(p => p.classList.remove('active'));

      item.classList.add('active');
      const targetPanel = document.getElementById(tabId);
      if (targetPanel) targetPanel.classList.add('active');
    });
  });

  // Load Settings
  async function initSettings() {
    try {
      const res = await invoke('load_settings');
      state.settings = res;
      applyTheme(res.theme);
      document.getElementById('settings-theme').value = res.theme;
      document.getElementById('settings-output-dir').value = res.output_directory;
      document.getElementById('settings-auto-open').checked = res.auto_open_output_folder;
      document.getElementById('split-output-dir').value = res.output_directory;
    } catch (err) {
      console.warn('Configuración cargada con valores por defecto:', err);
    }
  }

  function applyTheme(theme) {
    if (theme === 'Light') {
      document.body.setAttribute('data-theme', 'light');
    } else if (theme === 'Dark') {
      document.body.setAttribute('data-theme', 'dark');
    } else {
      // Default to Dark or match system
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      document.body.setAttribute('data-theme', prefersDark ? 'dark' : 'light');
    }
  }

  document.getElementById('btn-settings-save').addEventListener('click', async () => {
    try {
      state.settings.theme = document.getElementById('settings-theme').value;
      state.settings.output_directory = document.getElementById('settings-output-dir').value;
      state.settings.auto_open_output_folder = document.getElementById('settings-auto-open').checked;

      await invoke('save_settings', { settings: state.settings });
      applyTheme(state.settings.theme);
      showModal('Configuración', 'Preferencias guardadas exitosamente.');
    } catch (err) {
      showModal('Error', `No se pudo guardar la configuración: ${err}`);
    }
  });

  document.getElementById('btn-settings-browse').addEventListener('click', async () => {
    const dir = await openFileDialog({ directory: true, title: 'Seleccionar carpeta de salida' });
    if (dir) {
      document.getElementById('settings-output-dir').value = dir;
      document.getElementById('split-output-dir').value = dir;
    }
  });

  // ==========================================
  // TAB 1: UNIR PDF (MERGE)
  // ==========================================
  const mergeFileList = document.getElementById('merge-file-list');
  const mergeEmpty = document.getElementById('merge-empty');
  const btnMergeExecute = document.getElementById('btn-merge-execute');

  function renderMergeList() {
    mergeFileList.innerHTML = '';
    if (state.merge.files.length === 0) {
      mergeEmpty.style.display = 'flex';
      btnMergeExecute.disabled = true;
      return;
    }

    mergeEmpty.style.display = 'none';
    btnMergeExecute.disabled = state.merge.files.length < 2;

    state.merge.files.forEach((file, index) => {
      const li = document.createElement('li');
      li.className = `file-item ${state.merge.selectedIndex === index ? 'selected' : ''}`;
      li.innerHTML = `
        <div class="file-info-main">
          <span style="font-size:16px;">📄</span>
          <div>
            <div class="file-name">${file.file_name}</div>
            <div class="file-meta">${file.page_count} pág(s) • ${file.file_size_formatted}</div>
          </div>
        </div>
        <span class="file-meta">${index + 1}</span>
      `;
      li.addEventListener('click', () => {
        state.merge.selectedIndex = index;
        renderMergeList();
      });
      mergeFileList.appendChild(li);
    });
  }

  document.getElementById('btn-merge-add').addEventListener('click', async () => {
    try {
      const selected = await openFileDialog({
        multiple: true,
        filters: [{ name: 'Documentos PDF', extensions: ['pdf'] }]
      });
      if (!selected || selected.length === 0) return;

      const paths = Array.isArray(selected) ? selected : [selected];
      setStatus('Cargando información de documentos...', true);

      for (const p of paths) {
        if (!state.merge.files.some(f => f.file_path === p)) {
          const info = await invoke('get_file_info', { filePath: p });
          state.merge.files.push(info);
        }
      }
      setStatus(`${state.merge.files.length} documento(s) en cola para unir.`);
      renderMergeList();
    } catch (err) {
      showModal('Error al agregar PDFs', String(err));
      setStatus('Error al cargar archivo.');
    }
  });

  document.getElementById('btn-merge-up').addEventListener('click', () => {
    const idx = state.merge.selectedIndex;
    if (idx > 0) {
      const temp = state.merge.files[idx];
      state.merge.files[idx] = state.merge.files[idx - 1];
      state.merge.files[idx - 1] = temp;
      state.merge.selectedIndex = idx - 1;
      renderMergeList();
    }
  });

  document.getElementById('btn-merge-down').addEventListener('click', () => {
    const idx = state.merge.selectedIndex;
    if (idx >= 0 && idx < state.merge.files.length - 1) {
      const temp = state.merge.files[idx];
      state.merge.files[idx] = state.merge.files[idx + 1];
      state.merge.files[idx + 1] = temp;
      state.merge.selectedIndex = idx + 1;
      renderMergeList();
    }
  });

  document.getElementById('btn-merge-remove').addEventListener('click', () => {
    const idx = state.merge.selectedIndex;
    if (idx >= 0) {
      state.merge.files.splice(idx, 1);
      state.merge.selectedIndex = -1;
      renderMergeList();
    }
  });

  document.getElementById('btn-merge-clear').addEventListener('click', () => {
    state.merge.files = [];
    state.merge.selectedIndex = -1;
    renderMergeList();
  });

  btnMergeExecute.addEventListener('click', async () => {
    if (state.merge.files.length < 2) return;
    try {
      const savePath = await saveFileDialog({
        title: 'Guardar PDF Unido',
        defaultPath: 'documento_unido.pdf',
        filters: [{ name: 'Documento PDF', extensions: ['pdf'] }]
      });
      if (!savePath) return;

      setStatus('Combinando documentos PDF...', true);
      const inputPaths = state.merge.files.map(f => f.file_path);
      const res = await invoke('merge_pdfs', { files: inputPaths, outputPath: savePath });

      setStatus('¡Documentos unidos con éxito!');
      showModal('Éxito', `Se unieron ${state.merge.files.length} archivos en:\n${res.output_path}\nTotal de páginas: ${res.total_pages}`);
    } catch (err) {
      setStatus('Error al unir documentos.');
      showModal('Error al unir', String(err));
    }
  });

  // ==========================================
  // TAB 2: DIVIDIR PDF (SPLIT)
  // ==========================================
  const btnSplitSelect = document.getElementById('btn-split-select');
  const splitFilename = document.getElementById('split-filename');
  const splitDetails = document.getElementById('split-details');
  const btnSplitExecute = document.getElementById('btn-split-execute');
  const splitRangesInput = document.getElementById('split-ranges-input');
  const splitRangesGroup = document.getElementById('split-ranges-group');

  document.querySelectorAll('input[name="split-mode"]').forEach(r => {
    r.addEventListener('change', (e) => {
      splitRangesGroup.style.display = e.target.value === 'ranges' ? 'block' : 'none';
    });
  });

  btnSplitSelect.addEventListener('click', async () => {
    try {
      const selected = await openFileDialog({
        multiple: false,
        filters: [{ name: 'Documento PDF', extensions: ['pdf'] }]
      });
      if (!selected) return;

      const path = Array.isArray(selected) ? selected[0] : selected;
      const count = await invoke('get_page_count', { filePath: path });
      state.split.filePath = path;
      state.split.pageCount = count;

      splitFilename.textContent = path.split(/[\\/]/).pop();
      splitDetails.textContent = `${count} página(s)`;
      splitRangesInput.value = count > 1 ? `1-${Math.min(2, count)}, ${Math.min(3, count)}` : '1';
      btnSplitExecute.disabled = false;
      setStatus(`PDF cargado con ${count} página(s).`);
    } catch (err) {
      showModal('Error al abrir PDF', String(err));
    }
  });

  document.getElementById('btn-split-browse-dir').addEventListener('click', async () => {
    const dir = await openFileDialog({ directory: true, title: 'Seleccionar carpeta de destino' });
    if (dir) {
      document.getElementById('split-output-dir').value = dir;
    }
  });

  btnSplitExecute.addEventListener('click', async () => {
    if (!state.split.filePath) return;
    const outDir = document.getElementById('split-output-dir').value || '.';
    const mode = document.querySelector('input[name="split-mode"]:checked').value;
    const splitAll = mode === 'all';
    const ranges = splitAll ? null : splitRangesInput.value;

    try {
      setStatus('Dividiendo documento...', true);
      const generated = await invoke('split_pdf', {
        inputPath: state.split.filePath,
        outputDir: outDir,
        ranges: ranges,
        splitAll: splitAll
      });

      setStatus(`¡División completada! ${generated.length} archivo(s) generados.`);
      showModal('Éxito', `Se han generado ${generated.length} archivo(s) PDF en:\n${outDir}`);
    } catch (err) {
      setStatus('Error al dividir PDF.');
      showModal('Error en división', String(err));
    }
  });

  // ==========================================
  // TAB 3: EDITAR PÁGINAS (EDIT)
  // ==========================================
  const btnEditSelect = document.getElementById('btn-edit-select');
  const pagesGrid = document.getElementById('pages-grid');
  const editEmpty = document.getElementById('edit-empty');
  const btnRotateLeft = document.getElementById('btn-edit-rotate-left');
  const btnRotateRight = document.getElementById('btn-edit-rotate-right');
  const btnDeletePage = document.getElementById('btn-edit-delete-page');
  const btnEditSave = document.getElementById('btn-edit-save');

  function renderPagesGrid() {
    pagesGrid.innerHTML = '';
    if (state.edit.pages.length === 0) {
      editEmpty.style.display = 'flex';
      btnRotateLeft.disabled = true;
      btnRotateRight.disabled = true;
      btnDeletePage.disabled = true;
      btnEditSave.disabled = true;
      return;
    }

    editEmpty.style.display = 'none';
    btnEditSave.disabled = false;

    state.edit.pages.forEach((p, idx) => {
      const card = document.createElement('div');
      card.className = `page-card ${state.edit.selectedIndex === idx ? 'selected' : ''}`;
      card.innerHTML = `
        <div class="page-preview-box" style="transform: rotate(${p.rotation}deg)">
          <span>${p.pageNum}</span>
        </div>
        <div class="page-title">Página ${p.pageNum}</div>
        <div class="page-rotation-badge">Rotación: ${p.rotation}°</div>
      `;
      card.addEventListener('click', () => {
        state.edit.selectedIndex = idx;
        btnRotateLeft.disabled = false;
        btnRotateRight.disabled = false;
        btnDeletePage.disabled = false;
        renderPagesGrid();
      });
      pagesGrid.appendChild(card);
    });
  }

  btnEditSelect.addEventListener('click', async () => {
    try {
      const selected = await openFileDialog({
        multiple: false,
        filters: [{ name: 'Documento PDF', extensions: ['pdf'] }]
      });
      if (!selected) return;

      const path = Array.isArray(selected) ? selected[0] : selected;
      const count = await invoke('get_page_count', { filePath: path });
      state.edit.filePath = path;
      state.edit.pageCount = count;
      state.edit.pages = Array.from({ length: count }, (_, i) => ({ pageNum: i + 1, rotation: 0 }));
      state.edit.selectedIndex = 0;

      renderPagesGrid();
      btnRotateLeft.disabled = false;
      btnRotateRight.disabled = false;
      btnDeletePage.disabled = false;
      setStatus(`Documento cargado con ${count} página(s).`);
    } catch (err) {
      showModal('Error al abrir PDF', String(err));
    }
  });

  btnRotateLeft.addEventListener('click', () => {
    const idx = state.edit.selectedIndex;
    if (idx >= 0 && state.edit.pages[idx]) {
      state.edit.pages[idx].rotation = (state.edit.pages[idx].rotation - 90 + 360) % 360;
      renderPagesGrid();
    }
  });

  btnRotateRight.addEventListener('click', () => {
    const idx = state.edit.selectedIndex;
    if (idx >= 0 && state.edit.pages[idx]) {
      state.edit.pages[idx].rotation = (state.edit.pages[idx].rotation + 90) % 360;
      renderPagesGrid();
    }
  });

  btnDeletePage.addEventListener('click', () => {
    const idx = state.edit.selectedIndex;
    if (idx >= 0 && state.edit.pages.length > 1) {
      state.edit.pages.splice(idx, 1);
      state.edit.selectedIndex = Math.min(idx, state.edit.pages.length - 1);
      renderPagesGrid();
      setStatus(`Página eliminada. Restan ${state.edit.pages.length} página(s).`);
    } else if (state.edit.pages.length <= 1) {
      showModal('Aviso', 'El documento no puede quedarse sin páginas.');
    }
  });

  btnEditSave.addEventListener('click', async () => {
    if (!state.edit.filePath || state.edit.pages.length === 0) return;
    try {
      const defaultName = `${state.edit.filePath.split(/[\\/]/).pop().replace('.pdf', '')}_editado.pdf`;
      const savePath = await saveFileDialog({
        title: 'Guardar PDF Modificado',
        defaultPath: defaultName,
        filters: [{ name: 'Documento PDF', extensions: ['pdf'] }]
      });
      if (!savePath) return;

      setStatus('Guardando documento organizado...', true);
      const pagesToKeep = state.edit.pages.map(p => p.pageNum);
      const rotations = {};
      state.edit.pages.forEach(p => {
        if (p.rotation !== 0) {
          rotations[p.pageNum] = p.rotation;
        }
      });

      await invoke('save_edited_pdf', {
        inputPath: state.edit.filePath,
        outputPath: savePath,
        pagesToKeep: pagesToKeep,
        rotations: rotations
      });

      setStatus('¡Documento editado guardado con éxito!');
      showModal('Éxito', `Archivo guardado en:\n${savePath}`);
    } catch (err) {
      setStatus('Error al guardar.');
      showModal('Error al guardar PDF', String(err));
    }
  });

  // ==========================================
  // TAB 4: COMPRIMIR PDF / ZIP
  // ==========================================
  const btnCompressSelect = document.getElementById('btn-compress-select');
  const compressFileInfo = document.getElementById('compress-file-info');
  const btnCompressExecute = document.getElementById('btn-compress-execute');
  const compressStats = document.getElementById('compress-stats');

  btnCompressSelect.addEventListener('click', async () => {
    try {
      const selected = await openFileDialog({
        multiple: false,
        filters: [{ name: 'Documento PDF', extensions: ['pdf'] }]
      });
      if (!selected) return;

      const path = Array.isArray(selected) ? selected[0] : selected;
      const info = await invoke('get_file_info', { filePath: path });
      state.compress.filePath = path;

      compressFileInfo.textContent = `${info.file_name} (${info.file_size_formatted})`;
      btnCompressExecute.disabled = false;
      compressStats.style.display = 'none';
      setStatus('Archivo listo para optimización.');
    } catch (err) {
      showModal('Error', String(err));
    }
  });

  btnCompressExecute.addEventListener('click', async () => {
    if (!state.compress.filePath) return;
    try {
      const defaultName = `${state.compress.filePath.split(/[\\/]/).pop().replace('.pdf', '')}_comprimido.pdf`;
      const savePath = await saveFileDialog({
        title: 'Guardar PDF Optimizado',
        defaultPath: defaultName,
        filters: [{ name: 'Documento PDF', extensions: ['pdf'] }]
      });
      if (!savePath) return;

      setStatus('Optimizando y comprimiendo flujos internos...', true);
      const res = await invoke('compress_pdf', {
        inputPath: state.compress.filePath,
        outputPath: savePath
      });

      compressStats.style.display = 'block';
      compressStats.innerHTML = `
        <strong>Compresión completada:</strong><br>
        Tamaño inicial: ${(res.original_size_bytes / 1024).toFixed(1)} KB<br>
        Tamaño final: ${(res.compressed_size_bytes / 1024).toFixed(1)} KB<br>
        Ahorro: <strong>${res.saved_percentage.toFixed(1)}%</strong>
      `;

      setStatus('¡Compresión finalizada exitosamente!');
      showModal('Éxito', `Documento optimizado con éxito en:\n${res.output_path}\nAhorro: ${res.saved_percentage.toFixed(1)}%`);
    } catch (err) {
      setStatus('Error al comprimir.');
      showModal('Error', String(err));
    }
  });

  // ZIP Packager
  const zipFileList = document.getElementById('zip-file-list');
  const btnZipAdd = document.getElementById('btn-zip-add');
  const btnZipClear = document.getElementById('btn-zip-clear');
  const btnZipExecute = document.getElementById('btn-zip-execute');
  const btnZipExtract = document.getElementById('btn-zip-extract');

  function renderZipList() {
    zipFileList.innerHTML = '';
    btnZipExecute.disabled = state.compress.zipFiles.length === 0;

    state.compress.zipFiles.forEach(f => {
      const li = document.createElement('li');
      li.className = 'file-item';
      li.innerHTML = `
        <div class="file-name">${f.file_name}</div>
        <div class="file-meta">${f.file_size_formatted}</div>
      `;
      zipFileList.appendChild(li);
    });
  }

  btnZipAdd.addEventListener('click', async () => {
    try {
      const selected = await openFileDialog({ multiple: true, title: 'Archivos para empaquetar en ZIP' });
      if (!selected) return;

      const paths = Array.isArray(selected) ? selected : [selected];
      for (const p of paths) {
        if (!state.compress.zipFiles.some(f => f.file_path === p)) {
          const info = await invoke('get_file_info', { filePath: p });
          state.compress.zipFiles.push(info);
        }
      }
      renderZipList();
    } catch (err) {
      showModal('Error al añadir archivos', String(err));
    }
  });

  btnZipClear.addEventListener('click', () => {
    state.compress.zipFiles = [];
    renderZipList();
  });

  btnZipExecute.addEventListener('click', async () => {
    if (state.compress.zipFiles.length === 0) return;
    try {
      const savePath = await saveFileDialog({
        title: 'Guardar Archivo ZIP',
        defaultPath: 'paquete_documentos.zip',
        filters: [{ name: 'Archivo ZIP', extensions: ['zip'] }]
      });
      if (!savePath) return;

      setStatus('Empaquetando archivos en ZIP...', true);
      const paths = state.compress.zipFiles.map(f => f.file_path);
      await invoke('create_zip_archive', { files: paths, outputZipPath: savePath });

      setStatus('¡Archivo ZIP generado exitosamente!');
      showModal('Éxito', `Archivo ZIP creado en:\n${savePath}`);
    } catch (err) {
      setStatus('Error al crear ZIP.');
      showModal('Error', String(err));
    }
  });

  btnZipExtract.addEventListener('click', async () => {
    try {
      const zipPath = await openFileDialog({
        multiple: false,
        title: 'Seleccionar archivo ZIP a descomprimir',
        filters: [{ name: 'Archivo ZIP', extensions: ['zip'] }]
      });
      if (!zipPath) return;

      const destDir = await openFileDialog({ directory: true, title: 'Carpeta donde descomprimir' });
      if (!destDir) return;

      setStatus('Descomprimiendo con verificación de seguridad...', true);
      const extracted = await invoke('extract_zip_archive', {
        zipPath: Array.isArray(zipPath) ? zipPath[0] : zipPath,
        destinationDir: destDir
      });

      setStatus('¡Descompresión completada con éxito!');
      showModal('Éxito', `Se extrajeron ${extracted.length} archivo(s) de manera segura.`);
    } catch (err) {
      setStatus('Error al descomprimir.');
      showModal('Error de Seguridad o Descompresión', String(err));
    }
  });

  // ==========================================
  // TAB 5: INCRUSTAR IMAGEN / SELLO (STAMP)
  // ==========================================
  const btnStampPdf = document.getElementById('btn-stamp-pdf');
  const stampPdfName = document.getElementById('stamp-pdf-name');
  const btnStampImg = document.getElementById('btn-stamp-img');
  const stampImgName = document.getElementById('stamp-img-name');
  const stampPreview = document.getElementById('stamp-img-preview-container');
  const btnStampExecute = document.getElementById('btn-stamp-execute');
  const stampPage = document.getElementById('stamp-page');

  btnStampPdf.addEventListener('click', async () => {
    try {
      const selected = await openFileDialog({
        multiple: false,
        filters: [{ name: 'Documento PDF', extensions: ['pdf'] }]
      });
      if (!selected) return;

      const path = Array.isArray(selected) ? selected[0] : selected;
      const count = await invoke('get_page_count', { filePath: path });
      state.stamp.pdfPath = path;
      state.stamp.pageCount = count;
      stampPdfName.textContent = path.split(/[\\/]/).pop();
      stampPage.max = count;

      if (state.stamp.imgPath) btnStampExecute.disabled = false;
    } catch (err) {
      showModal('Error', String(err));
    }
  });

  btnStampImg.addEventListener('click', async () => {
    try {
      const selected = await openFileDialog({
        multiple: false,
        filters: [{ name: 'Imágenes', extensions: ['png', 'jpg', 'jpeg', 'bmp'] }]
      });
      if (!selected) return;

      const path = Array.isArray(selected) ? selected[0] : selected;
      state.stamp.imgPath = path;
      stampImgName.textContent = path.split(/[\\/]/).pop();
      stampPreview.innerHTML = `<img src="file://${path.replace(/\\/g, '/')}" alt="Previsualización">`;

      if (state.stamp.pdfPath) btnStampExecute.disabled = false;
    } catch (err) {
      showModal('Error', String(err));
    }
  });

  btnStampExecute.addEventListener('click', async () => {
    if (!state.stamp.pdfPath || !state.stamp.imgPath) return;
    try {
      const defaultName = `${state.stamp.pdfPath.split(/[\\/]/).pop().replace('.pdf', '')}_sellado.pdf`;
      const savePath = await saveFileDialog({
        title: 'Guardar PDF con Sello',
        defaultPath: defaultName,
        filters: [{ name: 'Documento PDF', extensions: ['pdf'] }]
      });
      if (!savePath) return;

      setStatus('Incrustando sello en el documento...', true);
      await invoke('add_image_to_pdf', {
        inputPdf: state.stamp.pdfPath,
        inputImage: state.stamp.imgPath,
        outputPath: savePath,
        page: parseInt(stampPage.value, 10),
        x: parseFloat(document.getElementById('stamp-x').value),
        y: parseFloat(document.getElementById('stamp-y').value),
        width: parseFloat(document.getElementById('stamp-width').value),
        height: parseFloat(document.getElementById('stamp-height').value)
      });

      setStatus('¡Imagen incrustada con éxito!');
      showModal('Éxito', `Documento guardado en:\n${savePath}`);
    } catch (err) {
      setStatus('Error al incrustar.');
      showModal('Error', String(err));
    }
  });

  // ==========================================
  // TAB 6: FIRMA DIGITAL (SIGN)
  // ==========================================
  const sigPad = new SignaturePad('signature-pad');
  const btnSignClear = document.getElementById('btn-sign-clear');
  const signStrokeSlider = document.getElementById('sign-stroke-width');
  const btnSignPdf = document.getElementById('btn-sign-pdf');
  const signPdfName = document.getElementById('sign-pdf-name');
  const signPage = document.getElementById('sign-page');
  const btnSignExecute = document.getElementById('btn-sign-execute');
  const btnSignLoadImg = document.getElementById('btn-sign-load-img');

  btnSignClear.addEventListener('click', () => {
    sigPad.clear();
    state.sign.customImgPath = null;
  });

  signStrokeSlider.addEventListener('input', (e) => {
    sigPad.setStrokeWidth(parseFloat(e.target.value));
  });

  btnSignLoadImg.addEventListener('click', async () => {
    try {
      const selected = await openFileDialog({
        multiple: false,
        filters: [{ name: 'Imagen de Firma', extensions: ['png', 'jpg', 'jpeg'] }]
      });
      if (!selected) return;

      const path = Array.isArray(selected) ? selected[0] : selected;
      state.sign.customImgPath = path;
      showModal('Firma Cargada', `Archivo seleccionado: ${path.split(/[\\/]/).pop()}`);
    } catch (err) {
      showModal('Error', String(err));
    }
  });

  btnSignPdf.addEventListener('click', async () => {
    try {
      const selected = await openFileDialog({
        multiple: false,
        filters: [{ name: 'Documento PDF', extensions: ['pdf'] }]
      });
      if (!selected) return;

      const path = Array.isArray(selected) ? selected[0] : selected;
      const count = await invoke('get_page_count', { filePath: path });
      state.sign.pdfPath = path;
      state.sign.pageCount = count;
      signPdfName.textContent = path.split(/[\\/]/).pop();
      signPage.max = count;
      btnSignExecute.disabled = false;
    } catch (err) {
      showModal('Error', String(err));
    }
  });

  btnSignExecute.addEventListener('click', async () => {
    if (!state.sign.pdfPath) return;

    let signatureData = state.sign.customImgPath;
    if (!signatureData) {
      if (sigPad.isEmpty()) {
        showModal('Aviso', 'Primero dibuja o carga una firma digital en el lienzo.');
        return;
      }
      signatureData = sigPad.toDataURL();
    }

    try {
      const defaultName = `${state.sign.pdfPath.split(/[\\/]/).pop().replace('.pdf', '')}_firmado.pdf`;
      const savePath = await saveFileDialog({
        title: 'Guardar PDF Firmado',
        defaultPath: defaultName,
        filters: [{ name: 'Documento PDF', extensions: ['pdf'] }]
      });
      if (!savePath) return;

      setStatus('Estampando firma digital en el documento...', true);

      // Posicionamiento inteligente por defecto
      const placement = document.getElementById('sign-placement').value;
      let x = 380, y = 50, w = 160, h = 75; // inferior derecha en coordenadas estándar PDF
      if (placement === 'bottom-left') {
        x = 50; y = 50;
      } else if (placement === 'bottom-center') {
        x = 220; y = 50;
      }

      await invoke('stamp_signature', {
        inputPdf: state.sign.pdfPath,
        signatureData: signatureData,
        outputPath: savePath,
        page: parseInt(signPage.value, 10),
        x: x,
        y: y,
        width: w,
        height: h
      });

      setStatus('¡Documento firmado exitosamente!');
      showModal('Éxito', `Documento firmado guardado en:\n${savePath}`);
    } catch (err) {
      setStatus('Error al firmar.');
      showModal('Error', String(err));
    }
  });

  // ==========================================
  // TAB 7: IMÁGENES A PDF (IMG2PDF)
  // ==========================================
  const img2pdfFileList = document.getElementById('img2pdf-file-list');
  const img2pdfEmpty = document.getElementById('img2pdf-empty');
  const btnImg2PdfExecute = document.getElementById('btn-img2pdf-execute');

  function renderImg2PdfList() {
    img2pdfFileList.innerHTML = '';
    if (state.img2pdf.files.length === 0) {
      img2pdfEmpty.style.display = 'flex';
      btnImg2PdfExecute.disabled = true;
      return;
    }

    img2pdfEmpty.style.display = 'none';
    btnImg2PdfExecute.disabled = false;

    state.img2pdf.files.forEach((file, index) => {
      const li = document.createElement('li');
      li.className = `file-item ${state.img2pdf.selectedIndex === index ? 'selected' : ''}`;
      li.innerHTML = `
        <div class="file-info-main">
          <span style="font-size:16px;">🖼️</span>
          <div>
            <div class="file-name">${file.file_name}</div>
            <div class="file-meta">${file.file_size_formatted}</div>
          </div>
        </div>
        <span class="file-meta">Página ${index + 1}</span>
      `;
      li.addEventListener('click', () => {
        state.img2pdf.selectedIndex = index;
        renderImg2PdfList();
      });
      img2pdfFileList.appendChild(li);
    });
  }

  document.getElementById('btn-img2pdf-add').addEventListener('click', async () => {
    try {
      const selected = await openFileDialog({
        multiple: true,
        filters: [{ name: 'Imágenes', extensions: ['png', 'jpg', 'jpeg', 'bmp'] }]
      });
      if (!selected) return;

      const paths = Array.isArray(selected) ? selected : [selected];
      for (const p of paths) {
        if (!state.img2pdf.files.some(f => f.file_path === p)) {
          const info = await invoke('get_file_info', { filePath: p });
          state.img2pdf.files.push(info);
        }
      }
      renderImg2PdfList();
    } catch (err) {
      showModal('Error', String(err));
    }
  });

  document.getElementById('btn-img2pdf-up').addEventListener('click', () => {
    const idx = state.img2pdf.selectedIndex;
    if (idx > 0) {
      const temp = state.img2pdf.files[idx];
      state.img2pdf.files[idx] = state.img2pdf.files[idx - 1];
      state.img2pdf.files[idx - 1] = temp;
      state.img2pdf.selectedIndex = idx - 1;
      renderImg2PdfList();
    }
  });

  document.getElementById('btn-img2pdf-down').addEventListener('click', () => {
    const idx = state.img2pdf.selectedIndex;
    if (idx >= 0 && idx < state.img2pdf.files.length - 1) {
      const temp = state.img2pdf.files[idx];
      state.img2pdf.files[idx] = state.img2pdf.files[idx + 1];
      state.img2pdf.files[idx + 1] = temp;
      state.img2pdf.selectedIndex = idx + 1;
      renderImg2PdfList();
    }
  });

  document.getElementById('btn-img2pdf-remove').addEventListener('click', () => {
    const idx = state.img2pdf.selectedIndex;
    if (idx >= 0) {
      state.img2pdf.files.splice(idx, 1);
      state.img2pdf.selectedIndex = -1;
      renderImg2PdfList();
    }
  });

  document.getElementById('btn-img2pdf-clear').addEventListener('click', () => {
    state.img2pdf.files = [];
    state.img2pdf.selectedIndex = -1;
    renderImg2PdfList();
  });

  btnImg2PdfExecute.addEventListener('click', async () => {
    if (state.img2pdf.files.length === 0) return;
    try {
      const savePath = await saveFileDialog({
        title: 'Guardar PDF de Imágenes',
        defaultPath: 'imagenes_convertidas.pdf',
        filters: [{ name: 'Documento PDF', extensions: ['pdf'] }]
      });
      if (!savePath) return;

      setStatus('Convirtiendo imágenes a documento PDF...', true);
      const paths = state.img2pdf.files.map(f => f.file_path);
      await invoke('convert_images_to_pdf', { images: paths, outputPath: savePath });

      setStatus('¡PDF de imágenes generado con éxito!');
      showModal('Éxito', `Documento generado en:\n${savePath}`);
    } catch (err) {
      setStatus('Error al convertir.');
      showModal('Error', String(err));
    }
  });

  // Initialization
  initSettings();
});
