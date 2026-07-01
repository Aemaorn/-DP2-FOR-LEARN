# Stage 1: Build
FROM node:22-alpine AS builder

# กำหนด working dir
WORKDIR /app

# คัดลอก package files และติดตั้ง dependencies (npm ci = ติดตั้งตาม package-lock.json เป๊ะ ทุก build)
COPY --chown=appuser:appgroup package*.json ./
RUN npm ci

# คัดลอก source code ทั้งหมดแล้ว build
COPY --chown=appuser:appgroup . .
RUN npm run build

# Stage 2: Serve ด้วย NGINX
FROM nginx:1.27.5-alpine

# ลบ default config และเพิ่ม config ที่ปลอดภัยกว่า
RUN rm /etc/nginx/conf.d/default.conf
COPY nginx.conf /etc/nginx/conf.d/default.conf

# คัดลอกไฟล์ build มาจาก stage ก่อนหน้า
COPY --from=builder /app/dist /usr/share/nginx/html

EXPOSE 80
COPY entrypoint.sh /usr/local/bin/entrypoint.sh
RUN chmod +x /usr/local/bin/entrypoint.sh
ENTRYPOINT ["/bin/sh", "-c"]
CMD ["entrypoint.sh"]
