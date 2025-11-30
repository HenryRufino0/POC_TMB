# Imagem base com Node
FROM node:20-alpine

# Pasta de trabalho dentro do container
WORKDIR /app

# Copia os arquivos de dependências
COPY package*.json ./

# Instala as dependências
RUN npm install

# Copia o restante do código
COPY . .

# Build da aplicação (Vite)
RUN npm run build

# Expõe a porta que o frontend vai usar
EXPOSE 3000

# Sobe o servidor de preview do Vite
CMD ["npm", "run", "preview", "--", "--host", "0.0.0.0", "--port", "3000"]
