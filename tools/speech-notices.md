# Little Notebook offline speech components

This directory is a private portable speech helper, launched by the native game only when narration is requested. Generated speech is local. Retain this complete directory when moving the app.

- CPython 3.12.10: PSF license; LICENSE.txt in this directory. Source: https://www.python.org/downloads/release/python-31210/
- Kokoro-82M weights and voices: Apache-2.0. https://huggingface.co/hexgrad/Kokoro-82M and https://github.com/thewh1teagle/kokoro-onnx/releases/tag/model-files-v1.0
- kokoro-onnx 0.6.1: MIT. https://github.com/thewh1teagle/kokoro-onnx
- Piper 1.4.2: GPL-3.0-or-later. Source/build instructions: https://github.com/OHF-Voice/piper1-gpl/tree/v1.4.2
- Kareem Arabic voice: model card retained in models/MODEL_CARD. https://huggingface.co/rhasspy/piper-voices/tree/main/ar/ar_JO/kareem/medium
- Piper includes Arabic tashkeel data/model and eSpeak NG. Preserve the package's included notices. Piper source above; eSpeak NG source: https://github.com/espeak-ng/espeak-ng
- phonemizer 3.4.0: GPL. https://github.com/bootphon/phonemizer
- espeakng-loader 0.2.4: loader/distributed eSpeak components. https://github.com/thewh1teagle/espeakng-loader
- ONNX Runtime 1.31.0: MIT. https://github.com/microsoft/onnxruntime
- NumPy 2.5.4: BSD with bundled-component notices. https://github.com/numpy/numpy
- SoundFile 0.14.0: BSD; libsndfile has its own included LGPL notices. https://github.com/bastibe/python-soundfile and https://github.com/libsndfile/libsndfile

Additional package notices/versions are in Lib/*.dist-info: attrs (MIT), cffi (MIT-0), cloudpickle and joblib (BSD), dlinfo (MIT), flatbuffers (Apache-2.0), packaging (Apache-2.0/BSD), pathvalidate (MIT), protobuf and pycparser (BSD), typing_extensions (PSF). Preserve their license directories and metadata.

The project's helper source is tools/speech_worker.py in the development workspace; a copy is worker.py here. Runtime preparation is tools/prepare_speech_runtime.py. Models and installed packages are unchanged upstream artifacts. The main native game communicates through local standard input/output; no accounts, credentials, remote speech service or listening socket are used.
