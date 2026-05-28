// @ts-check

import globals from 'globals';
import eslint from '@eslint/js';
import tsEslint from 'typescript-eslint';
import { defineConfig } from 'eslint/config';

export default defineConfig([
    {
        files: ['**/*.jslib', '**/*.jspre'],
        ignores: ['**/*.js'],
        languageOptions: {
            ecmaVersion: 2017,
            sourceType: 'script',
            globals: {
                ...globals.browser,
                Module: 'readonly',
                getWasmTableEntry: 'readonly',
                UTF8ToString: 'readonly',
                stringToUTF8: 'readonly',
                lengthBytesUTF8: 'readonly',
                HEAPU8: 'readonly',
                _malloc: 'readonly',
                _free: 'readonly',
                autoAddDeps: 'readonly',
                mergeInto: 'readonly',
                LibraryManager: 'readonly',
            }
        },
        plugins: {
            eslint,
            tsEslint,
        },
        rules: {
            ...eslint.configs.recommended.rules,
            ...tsEslint.configs.recommended.values().flatMap(config => config.rules),
            'no-unused-vars': ['error', {
                vars: 'all',
                varsIgnorePattern: '^_',
                args: 'after-used',
                argsIgnorePattern: '^_',
                caughtErrors: 'all',
                caughtErrorsIgnorePattern: '^_',
                ignoreRestSiblings: false,
                ignoreUsingDeclarations: false,
                reportUsedIgnorePattern: false,
            }],
            'no-undef-init': 'error',
        }
    },
]);
